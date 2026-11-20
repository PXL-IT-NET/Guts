using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Guts.Api.ScheduledJobs;

public class RecurringJobListener(
    ILogger<RecurringJobListener> logger,
    TimeProvider timeProvider = null) : IJobListener
{
    public const string MaxRetriesKey = "MaxRetries";
    public const string RetryDelaySecondsKey = "RetryDelaySeconds";
    public const string RetryAttemptKey = "RetryAttempt";

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public string Name => nameof(RecurringJobListener);

    public ValueTask JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            int retryAttempt = GetInt(context.MergedJobDataMap, RetryAttemptKey);
            logger.LogInformation(
                "Starting scheduled job {JobName} (FireInstanceId: {FireInstanceId}, RetryAttempt: {RetryAttempt}).",
                context.JobDetail.Key.Name,
                context.FireInstanceId,
                retryAttempt);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("Scheduled job {JobName} execution was vetoed.", context.JobDetail.Key.Name);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException jobException,
        CancellationToken cancellationToken = default)
    {
        string jobName = context.JobDetail.Key.Name;

        if (jobException == null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Completed scheduled job {JobName} in {ElapsedMs} ms.",
                    jobName,
                    context.JobRunTime.TotalMilliseconds);
            }

            return;
        }

        if (cancellationToken.IsCancellationRequested || jobException.InnerException is OperationCanceledException)
        {
            logger.LogWarning("Scheduled job {JobName} was cancelled.", jobName);
            return;
        }

        int maxRetries = GetInt(context.JobDetail.JobDataMap, MaxRetriesKey);
        int retryDelaySeconds = GetInt(context.JobDetail.JobDataMap, RetryDelaySecondsKey);
        int retryAttempt = GetInt(context.MergedJobDataMap, RetryAttemptKey);

        if (maxRetries > 0 && retryAttempt < maxRetries)
        {
            int nextAttempt = retryAttempt + 1;
            TimeSpan delay = TimeSpan.FromSeconds(Math.Max(1, retryDelaySeconds));
            DateTimeOffset retryAt = _timeProvider.GetUtcNow().Add(delay);

            logger.LogError(
                jobException,
                "Scheduled job {JobName} failed (attempt {Attempt}/{MaxRetries}). Scheduling retry at {RetryAtUtc}.",
                jobName,
                nextAttempt,
                maxRetries,
                retryAt);

            ITrigger retryTrigger = TriggerBuilder.Create()
                .WithIdentity($"{jobName}-retry-{nextAttempt}-{Guid.NewGuid():N}", context.JobDetail.Key.Group)
                .ForJob(context.JobDetail.Key)
                .UsingJobData(RetryAttemptKey, nextAttempt.ToString(CultureInfo.InvariantCulture))
                .StartAt(retryAt)
                .Build();

            await context.Scheduler.ScheduleJob(retryTrigger, cancellationToken: cancellationToken);
            return;
        }

        logger.LogError(
            jobException,
            "Scheduled job {JobName} failed permanently after {RetryAttempt} retry attempt(s) (MaxRetries: {MaxRetries}).",
            jobName,
            retryAttempt,
            maxRetries);
    }

    private static int GetInt(JobDataMap dataMap, string key)
    {
        if (dataMap == null || !dataMap.TryGetValue(key, out object rawValue) || rawValue == null)
        {
            return 0;
        }

        if (rawValue is int intValue)
        {
            return intValue;
        }

        return int.TryParse(rawValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : 0;
    }
}
