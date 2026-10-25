using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Guts.Business.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Guts.Api.BackgroundServices
{
    public class SprintLeaderboardBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<SprintLeaderboardBackgroundService> _logger;

        public SprintLeaderboardBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<SprintLeaderboardBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public static DateTime GetNextMondayMorningTrigger(DateTime fromUtc)
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)fromUtc.DayOfWeek + 7) % 7;
            DateTime target = fromUtc.Date.AddDays(daysUntilMonday).AddMinutes(1);

            if (target <= fromUtc)
            {
                target = target.AddDays(7);
            }

            return target;
        }

        public async Task ExecuteSprintAwardingAsync(DateTime executionTime, CancellationToken cancellationToken = default)
        {
            DateTime previousWeekDate = executionTime.Date.AddDays(-1);
            int year = ISOWeek.GetYear(previousWeekDate);
            int weekNumber = ISOWeek.GetWeekOfYear(previousWeekDate);

            await ExecuteSprintAwardingAsync(year, weekNumber, cancellationToken);
        }

        public async Task ExecuteSprintAwardingAsync(int year, int weekNumber, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting weekly sprint badge awarding for Year {Year}, Week {WeekNumber}", year, weekNumber);
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var gamificationService = scope.ServiceProvider.GetRequiredService<IGamificationService>();
                await gamificationService.AwardWeeklySprintBadgesAsync(year, weekNumber);
            }
            _logger.LogInformation("Completed weekly sprint badge awarding for Year {Year}, Week {WeekNumber}", year, weekNumber);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SprintLeaderboardBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.UtcNow;
                DateTime nextRun = GetNextMondayMorningTrigger(now);
                TimeSpan delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next sprint badge awarding scheduled for {NextRun} (in {Delay})", nextRun, delay);
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
                    await ExecuteSprintAwardingAsync(DateTime.UtcNow, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while awarding weekly sprint badges.");
                }
            }

            _logger.LogInformation("SprintLeaderboardBackgroundService stopped.");
        }
    }
}

