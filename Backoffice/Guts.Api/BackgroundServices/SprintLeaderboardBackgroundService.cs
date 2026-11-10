using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services;
using Guts.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Guts.Api.BackgroundServices;

public class SprintLeaderboardBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SprintLeaderboardBackgroundService> logger)
    : BackgroundService
{
    private (int Year, int WeekNumber)? _lastSuccessfulWeek;
    private bool _lastRunFailed;

    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromHours(1);
    public (int Year, int WeekNumber)? LastSuccessfulWeek => _lastSuccessfulWeek;
    public bool LastRunFailed => _lastRunFailed;

    public static DateTime GetNextMondayMorningTrigger(DateTime fromUtc)
    {
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)fromUtc.DayOfWeek + 7) % 7;
        DateTime target = fromUtc.Date.AddDays(daysUntilMonday).AddMinutes(1);

        if (target <= fromUtc)
        {
            target = target.AddDays(7);
        }

        return target;
    }

    public static (int Year, int WeekNumber) GetPreviousCompletedWeek(DateTime fromUtc)
    {
        var prevWeek = IsoWeek.From(fromUtc).Previous();
        return (prevWeek.Year, prevWeek.WeekNumber);
    }

    public async Task ExecuteSprintAwardingAsync(DateTime executionTime, CancellationToken cancellationToken = default)
    {
        var (year, weekNumber) = GetPreviousCompletedWeek(executionTime);
        await ExecuteSprintAwardingAsync(year, weekNumber, cancellationToken);
    }

    public async Task ExecuteSprintAwardingAsync(int year, int weekNumber, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting weekly sprint badge awarding for Year {Year}, Week {WeekNumber}", year, weekNumber);
        }

        try
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var gamificationService = scope.ServiceProvider.GetRequiredService<IGamificationService>();
                await gamificationService.AwardWeeklySprintBadgesAsync(year, weekNumber);
            }

            _lastSuccessfulWeek = (year, weekNumber);
            _lastRunFailed = false;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Completed weekly sprint badge awarding for Year {Year}, Week {WeekNumber}", year, weekNumber);
            }
        }
        catch (Exception)
        {
            _lastRunFailed = true;
            throw;
        }
    }

    public async Task<bool> RunAwardingSafelyAsync(DateTime executionTime, CancellationToken cancellationToken = default)
    {
        var (year, weekNumber) = GetPreviousCompletedWeek(executionTime);

        if (!_lastRunFailed && _lastSuccessfulWeek == (year, weekNumber))
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Sprint badge awarding for Year {Year}, Week {WeekNumber} has already completed successfully. Skipping.", year, weekNumber);
            }
            return true;
        }

        try
        {
            await ExecuteSprintAwardingAsync(year, weekNumber, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _lastRunFailed = true;
            logger.LogError(ex, "An error occurred while awarding weekly sprint badges for Year {Year}, Week {WeekNumber}.", year, weekNumber);
            return false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SprintLeaderboardBackgroundService started.");

        await RunAwardingSafelyAsync(DateTime.UtcNow, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool shouldExecute = await WaitUntilNextRunAsync(DateTime.UtcNow, stoppingToken);
            if (!shouldExecute || stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunAwardingSafelyAsync(DateTime.UtcNow, stoppingToken);
        }

        logger.LogInformation("SprintLeaderboardBackgroundService stopped.");
    }

    private async Task<bool> WaitUntilNextRunAsync(DateTime now, CancellationToken stoppingToken)
    {
        DateTime nextMonday = GetNextMondayMorningTrigger(now);
        DateTime nextRun = _lastRunFailed ? now.Add(RetryInterval) : nextMonday;
        TimeSpan delay = nextRun - now;

        if (delay <= TimeSpan.Zero)
        {
            return true;
        }

        LogScheduledRun(nextRun, delay);

        try
        {
            await Task.Delay(delay, stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private void LogScheduledRun(DateTime nextRun, TimeSpan delay)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        if (_lastRunFailed)
        {
            logger.LogInformation("Last sprint badge awarding failed. Retrying in {Delay} (at {NextRun})", delay, nextRun);
        }
        else
        {
            logger.LogInformation("Next sprint badge awarding scheduled for {NextRun} (in {Delay})", nextRun, delay);
        }
    }
}
