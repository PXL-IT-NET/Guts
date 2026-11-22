using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Guts.Api.ScheduledJobs;

[ExcludeFromCodeCoverage]
public static class ScheduledJobsServiceCollectionExtensions
{
    public static IServiceCollection AddScheduledJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ScheduledJobsOptions>(configuration.GetSection(ScheduledJobsOptions.SectionName));
        services.AddSingleton<IQuartzDashboardTicketStore, MemoryQuartzDashboardTicketStore>();

        Assembly apiAssembly = typeof(IRecurringJob).Assembly;
        var jobDescriptors = apiAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IRecurringJob).IsAssignableFrom(t))
            .Select(RecurringJobDescriptor.FromType)
            .ToList();

        foreach (RecurringJobDescriptor descriptor in jobDescriptors)
        {
            services.AddSingleton(descriptor);
        }

        string connectionString = configuration.GetConnectionString("GutsDatabase");

        services.AddQuartz(q =>
        {
            q.AddJobListener<RecurringJobListener>();
            q.UseExecutionLogCapture();

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                q.UsePersistentStore(store =>
                {
                    store.ConfigureStore(ado =>
                    {
                        ado.TablePrefix = "quartz.QRTZ_";
                        ado.StoreJobDataAsStrings = true;
                    });
                    store.UseSqlServer(connectionString);
                    store.UseSystemTextJsonSerializer();
                    store.UseClustering();
                    store.UseExecutionHistory();
                });
            }
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddQuartzDashboard(options =>
        {
            options.AuthorizationPolicy = ApiConstants.QuartzDashboardAdminPolicy;
            options.IsJobTypeAllowed = typeName =>
                !string.IsNullOrWhiteSpace(typeName) &&
                typeName.StartsWith("Guts.Api.ScheduledJobs.", StringComparison.Ordinal);
        });

        services.AddHostedService<ScheduledJobsSyncHostedService>();

        return services;
    }
}
