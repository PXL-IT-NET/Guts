using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Guts.Api.BackgroundServices;

public class StreakReminderBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration,
    ILogger<StreakReminderBackgroundService> logger,
    TimeProvider timeProvider = null)
    : BackgroundService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public StreakReminderBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<StreakReminderBackgroundService> logger,
        TimeProvider timeProvider = null)
        : this(serviceScopeFactory, null, logger, timeProvider)
    {
    }

    private static DateTime GetNextThursdayMiddayTrigger(DateTime fromUtc)
    {
        int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)fromUtc.DayOfWeek + 7) % 7;
        DateTime target = fromUtc.Date.AddDays(daysUntilThursday).AddHours(12);

        if (target <= fromUtc)
        {
            target = target.AddDays(7);
        }

        return target;
    }

    private async Task ExecuteStreakRemindersAsync(DateTime executionTime, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting weekly streak reminder dispatch for {ExecutionTime}", executionTime);
        }

        using (var scope = serviceScopeFactory.CreateScope())
        {
            var streakReminderService = scope.ServiceProvider.GetRequiredService<IStreakReminderService>();
            int sentCount = await streakReminderService.SendStreakRemindersAsync(executionTime, cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Dispatched {SentCount} streak reminder emails.", sentCount);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool isEnabled = configuration?.GetValue<bool?>("StreakReminders:Enabled") ?? true;
        if (!isEnabled)
        {
            logger.LogInformation("StreakReminderBackgroundService is disabled by configuration.");
            return;
        }

        logger.LogInformation("StreakReminderBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
            DateTime nextRun = GetNextThursdayMiddayTrigger(now);
            TimeSpan delay = nextRun - now;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Next streak reminder dispatch scheduled for {NextRun} (in {Delay})", nextRun, delay);
            }

            try
            {
                await Task.Delay(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ExecuteStreakRemindersAsync(_timeProvider.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while sending weekly streak reminders.");
            }
        }

        logger.LogInformation("StreakReminderBackgroundService stopped.");
    }
}
