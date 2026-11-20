using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Guts.Api.ScheduledJobs;

public class ScheduledJobsSyncHostedService(
    ISchedulerFactory schedulerFactory,
    IEnumerable<RecurringJobDescriptor> descriptors,
    IOptions<ScheduledJobsOptions> options,
    ILogger<ScheduledJobsSyncHostedService> logger) : IHostedService
{
    public const string DefaultGroup = "GutsRecurring";

    private readonly IReadOnlyList<RecurringJobDescriptor> _descriptors = descriptors.ToList();
    private readonly ScheduledJobsOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ValidateDescriptors();

        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        foreach (RecurringJobDescriptor descriptor in _descriptors)
        {
            await SyncJobAsync(scheduler, descriptor, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidateDescriptors()
    {
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (RecurringJobDescriptor descriptor in _descriptors)
        {
            if (string.IsNullOrWhiteSpace(descriptor.JobName))
            {
                throw new InvalidOperationException($"Recurring job '{descriptor.JobType.FullName}' must define a non-empty JobName.");
            }

            if (!seenNames.Add(descriptor.JobName))
            {
                throw new InvalidOperationException($"Duplicate recurring job name '{descriptor.JobName}' detected.");
            }
        }
    }

    private async Task SyncJobAsync(
        IScheduler scheduler,
        RecurringJobDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        _options.Jobs.TryGetValue(descriptor.JobName, out ScheduledJobConfig jobConfig);

        bool isEnabled = jobConfig?.Enabled ?? true;
        string cronExpression = string.IsNullOrWhiteSpace(jobConfig?.CronExpression)
            ? descriptor.DefaultCronExpression
            : jobConfig.CronExpression;

        if (!CronExpression.TryParse(cronExpression, out _))
        {
            throw new InvalidOperationException(
                $"Invalid cron expression '{cronExpression}' configured for recurring job '{descriptor.JobName}'.");
        }

        string timeZoneId = ResolveTimeZoneId(jobConfig);
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var jobKey = new JobKey(descriptor.JobName, DefaultGroup);
        var triggerKey = new TriggerKey($"{descriptor.JobName}-trigger", DefaultGroup);

        IJobDetail jobDetail = JobBuilder.Create()
            .OfType(descriptor.JobType)
            .WithIdentity(jobKey)
            .WithDescription(descriptor.Description)
            .StoreDurably()
            .RequestRecovery()
            .UsingJobData(RecurringJobListener.MaxRetriesKey, descriptor.MaxRetries.ToString(CultureInfo.InvariantCulture))
            .UsingJobData(RecurringJobListener.RetryDelaySecondsKey, ((int)descriptor.RetryDelay.TotalSeconds).ToString(CultureInfo.InvariantCulture))
            .Build();

        await scheduler.AddJob(jobDetail, new AddJobOptions { Replace = true }, cancellationToken);

        ITrigger desiredTrigger = BuildCronTrigger(triggerKey, jobKey, cronExpression, timeZone, descriptor.MisfirePolicy);
        ITrigger existingTrigger = await scheduler.GetTrigger(triggerKey, cancellationToken);

        if (existingTrigger == null)
        {
            await scheduler.ScheduleJob(desiredTrigger, cancellationToken: cancellationToken);
        }
        else if (TriggerConfigurationChanged(existingTrigger, cronExpression, timeZone, descriptor.MisfirePolicy))
        {
            await scheduler.RescheduleJob(triggerKey, desiredTrigger, cancellationToken);
        }

        if (!isEnabled)
        {
            await scheduler.PauseJob(jobKey, cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Scheduled job {JobName} is disabled by configuration and has been paused.", descriptor.JobName);
            }
        }
        else
        {
            TriggerState state = await scheduler.GetTriggerState(triggerKey, cancellationToken);
            if (state == TriggerState.Paused)
            {
                await scheduler.ResumeJob(jobKey, cancellationToken);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Synchronized scheduled job {JobName} with cron '{CronExpression}' ({TimeZoneId}, Misfire: {MisfirePolicy}).",
                    descriptor.JobName,
                    cronExpression,
                    timeZone.Id,
                    descriptor.MisfirePolicy);
            }
        }
    }

    private string ResolveTimeZoneId(ScheduledJobConfig jobConfig)
    {
        if (!string.IsNullOrWhiteSpace(jobConfig?.TimeZoneId))
        {
            return jobConfig.TimeZoneId;
        }

        if (!string.IsNullOrWhiteSpace(_options.DefaultTimeZoneId))
        {
            return _options.DefaultTimeZoneId;
        }

        return ScheduledJobsOptions.DefaultBrusselsTimeZoneId;
    }

    private static ITrigger BuildCronTrigger(
        TriggerKey triggerKey,
        JobKey jobKey,
        string cronExpression,
        TimeZoneInfo timeZone,
        MisfirePolicy misfirePolicy)
    {
        CronTriggerMisfireInstruction misfireInstruction = ToQuartzMisfireInstruction(misfirePolicy);

        return TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .WithCronSchedule(cronExpression, builder =>
            {
                builder.InTimeZone(timeZone);
                builder.WithMisfireInstruction(misfireInstruction);
            })
            .Build();
    }

    private static bool TriggerConfigurationChanged(
        ITrigger existingTrigger,
        string desiredCronExpression,
        TimeZoneInfo desiredTimeZone,
        MisfirePolicy desiredMisfirePolicy)
    {
        if (existingTrigger is not ICronTrigger existingCron)
        {
            return true;
        }

        CronTriggerMisfireInstruction desiredMisfireInstruction = ToQuartzMisfireInstruction(desiredMisfirePolicy);

        return !string.Equals(existingCron.CronExpressionString, desiredCronExpression, StringComparison.Ordinal)
               || !string.Equals(existingCron.TimeZone?.Id, desiredTimeZone.Id, StringComparison.Ordinal)
               || existingCron.MisfireInstruction != desiredMisfireInstruction;
    }

    private static CronTriggerMisfireInstruction ToQuartzMisfireInstruction(MisfirePolicy misfirePolicy)
    {
        return misfirePolicy == MisfirePolicy.DoNothing
            ? CronTriggerMisfireInstruction.DoNothing
            : CronTriggerMisfireInstruction.FireAndProceed;
    }
}
