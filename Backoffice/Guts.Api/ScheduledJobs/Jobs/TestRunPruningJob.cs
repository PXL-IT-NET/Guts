using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Guts.Api.ScheduledJobs.Jobs;

[DisallowConcurrentExecution]
public class TestRunPruningJob(
    ITestRunPruningService testRunPruningService,
    IConfiguration configuration,
    ILogger<TestRunPruningJob> logger) : IRecurringJob
{
    public static string JobName => "TestRunPruning";
    public static string Description => "Prunes redundant TestRuns and TestResults in the active academic period that delivered no change in cumulative pass/fail test outcomes.";
    public static string DefaultCronExpression => "0 0 3 ? * SUN";
    public static MisfirePolicy MisfirePolicy => MisfirePolicy.FireAndProceed;
    public static int MaxRetries => 3;
    public static TimeSpan RetryDelay => TimeSpan.FromMinutes(15);

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        int batchSize = configuration?.GetValue<int?>("TestRunPruning:BatchSize")
                        ?? ITestRunPruningService.DefaultBatchSize;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting TestRunPruningJob (batch size: {BatchSize}).", batchSize);
        }

        await testRunPruningService.PruneRedundantTestRunsAsync(batchSize, cancellationToken);
    }
}
