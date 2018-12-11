using Google.Cloud.Diagnostics.AspNetCore;
using Guts.Data;
using Guts.Domain;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

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
            var builder =  WebHost.CreateDefaultBuilder(args).ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddEventSourceLogger();
                logging.AddConsole();
                logging.AddDebug();
            }).UseStartup<Startup>();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProduction = environment == EnvironmentName.Production;

            if (isProduction)
            {
                builder = builder.UseGoogleDiagnostics();
            }

            return builder;
        }
    }
}
