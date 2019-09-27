using Guts.Data;
using Guts.Domain;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Logging.EventLog;

namespace Guts.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<GutsContext>();
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Program");
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<Role>>();
                logger.LogInformation(1,"Starting Guts Api...");
                try
                {
                    var initializer = new GutsDbInitializer(context, logger, userManager, roleManager);

                    initializer.DoAutomaticMigrations();
                    initializer.Seed();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args)
                .UseIISIntegration()
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddEventLog(new EventLogSettings
                    {
                        SourceName = "Guts" //Attention: make sure the registry of the host contains a key "Guts" in HKEY_LOCAL_MACHINE -> SYSTEM -> CurrentControlSet -> Services -> EventLog -> Application
                    });
                    logging.AddDebug();
                }).UseStartup<Startup>();

            return builder;
        }
    }
}
