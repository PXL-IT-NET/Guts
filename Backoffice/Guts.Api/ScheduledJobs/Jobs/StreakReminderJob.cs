using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Guts.Api.ScheduledJobs.Jobs;

[DisallowConcurrentExecution]
public class StreakReminderJob(
    IStreakReminderService streakReminderService,
    ILogger<StreakReminderJob> logger) : IRecurringJob
{
    public static string JobName => "StreakReminder";
    public static string Description => "Sends weekly Thursday streak reminder emails to students at risk of losing their coding streak.";
    public static string DefaultCronExpression => "0 0 12 ? * THU";
    public static MisfirePolicy MisfirePolicy => MisfirePolicy.FireAndProceed;
    public static int MaxRetries => 3;
    public static TimeSpan RetryDelay => TimeSpan.FromMinutes(15);

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        DateTime executionTimeUtc = context.FireTimeUtc.UtcDateTime;
        int sentCount = await streakReminderService.SendStreakRemindersAsync(executionTimeUtc, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Dispatched {SentCount} streak reminder emails.", sentCount);
        }
    }
}
