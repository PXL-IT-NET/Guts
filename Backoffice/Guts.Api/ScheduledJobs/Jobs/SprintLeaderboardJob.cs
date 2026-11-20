using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services;
using Guts.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Guts.Api.ScheduledJobs.Jobs;

[DisallowConcurrentExecution]
public class SprintLeaderboardJob(
    IGamificationService gamificationService,
    ILogger<SprintLeaderboardJob> logger) : IRecurringJob
{
    public static string JobName => "SprintLeaderboard";
    public static string Description => "Awards weekly sprint badges and evaluates Hall of Fame rank defense for the previous completed ISO week.";
    public static string DefaultCronExpression => "0 1 0 ? * MON";
    public static MisfirePolicy MisfirePolicy => MisfirePolicy.FireAndProceed;
    public static int MaxRetries => 3;
    public static TimeSpan RetryDelay => TimeSpan.FromHours(1);

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        TimeZoneInfo timeZone = (context.Trigger as ICronTrigger)?.TimeZone
            ?? TimeZoneInfo.FindSystemTimeZoneById(ScheduledJobsOptions.DefaultBrusselsTimeZoneId);

        DateTime localFireTime = TimeZoneInfo.ConvertTimeFromUtc(context.FireTimeUtc.UtcDateTime, timeZone);
        IsoWeek previousWeek = IsoWeek.From(localFireTime).Previous();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Awarding weekly sprint badges for Year {Year}, Week {WeekNumber} (local fire time: {LocalFireTime} {TimeZoneId}).",
                previousWeek.Year,
                previousWeek.WeekNumber,
                localFireTime,
                timeZone.Id);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await gamificationService.AwardWeeklySprintBadgesAsync(previousWeek.Year, previousWeek.WeekNumber);
    }
}
