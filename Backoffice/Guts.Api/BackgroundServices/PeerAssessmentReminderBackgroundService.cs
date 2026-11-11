using System;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Services.Assessment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Guts.Api.BackgroundServices;

public class PeerAssessmentReminderBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration,
    ILogger<PeerAssessmentReminderBackgroundService> logger,
    TimeProvider timeProvider = null)
    : BackgroundService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(configuration?.GetValue<int?>("PeerAssessmentReminders:CheckIntervalInHours") ?? 2);

    private async Task ExecuteRemindersAsync(DateTime executionTime, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting peer assessment reminder check for {ExecutionTime}", executionTime);
        }

        using var scope = serviceScopeFactory.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IPeerAssessmentReminderService>();
        int sentCount = await reminderService.SendAssessmentRemindersAsync(executionTime, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Dispatched {SentCount} peer assessment reminder emails.", sentCount);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool isEnabled = configuration?.GetValue<bool?>("PeerAssessmentReminders:Enabled") ?? true;
        if (!isEnabled)
        {
            logger.LogInformation("PeerAssessmentReminderBackgroundService is disabled by configuration.");
            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("PeerAssessmentReminderBackgroundService started with interval {Interval}.", _checkInterval);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ExecuteRemindersAsync(_timeProvider.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while dispatching peer assessment reminder emails.");
            }
        }

        logger.LogInformation("PeerAssessmentReminderBackgroundService stopped.");
    }
}
