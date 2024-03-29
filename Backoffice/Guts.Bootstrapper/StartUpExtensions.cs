﻿using System;
using System.Linq;
using System.Reflection;
using Guts.Business.Captcha;
using Guts.Business.Communication;
using Guts.Business.Security;
using Guts.Business.Services;
using Guts.Infrastructure.Repositories;
using Guts.Domain.ExamAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Guts.Infrastructure;
using Guts.Infrastructure.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Guts.Bootstrapper
{
    public static class StartUpExtensions
    {
        public static void AddDomainLayerServices(this IServiceCollection services)
        {
            Assembly domainAssembly = typeof(ExamPart).Assembly;

            //register domain factories
            services.RegisterTypesWhoseNameEndsWith("Factory", domainAssembly, ServiceLifetime.Singleton);
        }

        public static void AddInfrastructureLayerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileLoggerOptions>(configuration.GetSection("Logging:FileLogger"));

            services.AddDbContext<GutsContext>(options =>
            {
                options
                    .UseLoggerFactory(
                        LoggerFactory.Create(builder =>
                        {
                            builder.AddFilter((category, level) =>
                                category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information);
                            builder.AddDebug();
                        }))
                    .UseSqlServer(configuration.GetConnectionString("GutsDatabase"), sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("Guts.Infrastructure");
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 15,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });

#if DEBUG
                options.EnableSensitiveDataLogging(true);
#endif
            });

            services.AddIdentity<User, Role>(options =>
                {
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 8;
                    options.Lockout.AllowedForNewUsers = true;

                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;

                    options.SignIn.RequireConfirmedEmail = true;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                })
                .AddEntityFrameworkStores<GutsContext>()
                .AddDefaultTokenProviders();

            Assembly dataAssembly = typeof(CourseDbRepository).Assembly;

            //register repositories
            services.RegisterTypesWhoseNameEndsWith("Repository", dataAssembly, ServiceLifetime.Scoped);
        }

        public static void AddBusinessLayerServices(this IServiceCollection services, IConfiguration configuration)
        {
            Assembly businessAssembly = typeof(CourseService).Assembly;

            //register converters
            services.RegisterTypesWhoseNameEndsWith("Converter", businessAssembly, ServiceLifetime.Singleton);

            //register services
            services.RegisterTypesWhoseNameEndsWith("Service", businessAssembly, ServiceLifetime.Scoped);

            //register loaders
            services.RegisterTypesWhoseNameEndsWith("Loader", businessAssembly, ServiceLifetime.Scoped);

            services.AddScoped<IHttpClient, HttpClientAdapter>();
            services.AddScoped<ICaptchaValidator>(provider =>
            {
                var captchaSection = configuration.GetSection("Captcha");
                var secret = captchaSection.GetValue<string>("secret");
                var validationUrl = captchaSection.GetValue<string>("validationUrl");
                return new GoogleCaptchaValidator(validationUrl, secret, provider.GetService<IHttpClient>());
            });

            services.AddScoped<ISmtpClient>(provider =>
            {
                var mailSection = configuration.GetSection("Mail");
                var smtpHost = mailSection.GetValue<string>("host");
                var port = mailSection.GetValue<int>("port");
                var fromEmail = mailSection.GetValue<string>("from");
                var password = mailSection.GetValue<string>("password");
                return new SmtpClientAdapter(smtpHost, port, fromEmail, password);
            });

            services.AddScoped<IMailSender>(provider =>
            {
                var mailSection = configuration.GetSection("Mail");
                var fromEmail = mailSection.GetValue<string>("from");
                var webAppBaseUrl = mailSection.GetValue<string>("webappbaseurl");
                return new MailSender(provider.GetService<ISmtpClient>(), fromEmail, webAppBaseUrl);
            });

            services.AddSingleton<ITokenAccessPassFactory>(provider =>
            {
                var tokenSection = configuration.GetSection("Tokens");
                var key = tokenSection.GetValue<string>("Key");
                var issuer = tokenSection.GetValue<string>("Issuer");
                var audience = tokenSection.GetValue<string>("Audience");
                var expirationTimeInMinutes = tokenSection.GetValue<int>("ExpirationTimeInMinutes");

                return new JwtSecurityTokenAccessPassFactory(key, issuer, audience, expirationTimeInMinutes);
            });
        }

        private static void RegisterTypesWhoseNameEndsWith(this IServiceCollection services, string classAndInterfaceNameEndsWith,
            Assembly targetAssembly,
            ServiceLifetime lifetime)
        {
            var registrations = from type in targetAssembly.GetTypes()
                                where type.Name.EndsWith(classAndInterfaceNameEndsWith) 
                                      && type.GetInterfaces().Any() 
                                      && !type.IsInterface
                                select new { ServiceType = type.GetInterfaces().First(i => i.Name.EndsWith(classAndInterfaceNameEndsWith)), ImplementationType = type };

            foreach (var registration in registrations)
            {
                services.Add(new ServiceDescriptor(registration.ServiceType, registration.ImplementationType, lifetime));
            }
        }
    }
}
