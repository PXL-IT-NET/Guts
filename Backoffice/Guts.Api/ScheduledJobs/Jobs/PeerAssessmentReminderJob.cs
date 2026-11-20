using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services.Assessment;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Guts.Api.ScheduledJobs.Jobs;

[DisallowConcurrentExecution]
public class PeerAssessmentReminderJob(
    IPeerAssessmentReminderService reminderService,
    ILogger<PeerAssessmentReminderJob> logger) : IRecurringJob
{
    public static string JobName => "PeerAssessmentReminder";
    public static string Description => "Checks open peer assessments approaching their deadline and sends reminder emails to pending evaluators.";
    public static string DefaultCronExpression => "0 0 */2 ? * *";
    public static MisfirePolicy MisfirePolicy => MisfirePolicy.DoNothing;
    public static int MaxRetries => 3;
    public static TimeSpan RetryDelay => TimeSpan.FromMinutes(15);

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        DateTime executionTimeUtc = context.FireTimeUtc.UtcDateTime;
        int sentCount = await reminderService.SendAssessmentRemindersAsync(executionTimeUtc, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Dispatched {SentCount} peer assessment reminder emails.", sentCount);
        }
    }
}
