using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Guts.Api.BackgroundServices;

public class StreakReminderBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<StreakReminderBackgroundService> logger)
    : BackgroundService
{
    public static DateTime GetNextThursdayMiddayTrigger(DateTime fromUtc)
    {
        int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)fromUtc.DayOfWeek + 7) % 7;
        DateTime target = fromUtc.Date.AddDays(daysUntilThursday).AddHours(12);

        if (target <= fromUtc)
        {
            target = target.AddDays(7);
        }

        return target;
    }

    public async Task ExecuteStreakRemindersAsync(DateTime executionTime, CancellationToken cancellationToken = default)
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
        logger.LogInformation("StreakReminderBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime now = DateTime.UtcNow;
            DateTime nextRun = GetNextThursdayMiddayTrigger(now);
            TimeSpan delay = nextRun - now;

            if (delay > TimeSpan.Zero)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Next streak reminder dispatch scheduled for {NextRun} (in {Delay})", nextRun, delay);
                }

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            try
            {
                await ExecuteStreakRemindersAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while sending weekly streak reminders.");
            }
        }

        logger.LogInformation("StreakReminderBackgroundService stopped.");
    }
}

