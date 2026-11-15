using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services.Deletion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Guts.Api.BackgroundServices;

public class DeletionBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IDeletionJobQueue deletionJobQueue,
    IConfiguration configuration,
    ILogger<DeletionBackgroundService> logger)
    : BackgroundService
{
    private readonly int _batchSize = configuration?.GetValue<int?>("Deletion:BatchSize") ?? IDeletionJobService.DefaultBatchSize;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool isEnabled = configuration?.GetValue<bool?>("Deletion:Enabled") ?? true;
        if (!isEnabled)
        {
            logger.LogInformation("DeletionBackgroundService is disabled by configuration.");
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("DeletionBackgroundService started with batch size {BatchSize}.", _batchSize);
        }

        await ProcessSafelyAsync(stoppingToken);

        await foreach (Guid _ in deletionJobQueue.DequeueAllAsync(stoppingToken))
        {
            await ProcessSafelyAsync(stoppingToken);
        }

        logger.LogInformation("DeletionBackgroundService stopped.");
    }

    private async Task ProcessSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var deletionJobService = scope.ServiceProvider.GetRequiredService<IDeletionJobService>();
            await deletionJobService.ProcessPendingJobsAsync(_batchSize, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down gracefully.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing chunked deletion jobs.");
        }
    }
}
