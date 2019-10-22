using System.Linq;
using System.Net;
using System.Reflection;
using Guts.Api.Models.Converters;
using Guts.Business.Captcha;
using Guts.Business.Communication;
using Guts.Business.Converters;
using Guts.Business.Security;
using Guts.Business.Services;
using Guts.Data.Repositories;
using Guts.Domain.ExamAggregate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Guts.Api.Extensions
{
    public static class StartUpExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            //register domain factories
            services.RegisterTypesWhoseNameEndsWith("Factory", typeof(ExamPart).Assembly, ServiceLifetime.Singleton);

            //register converters in api project
            services.RegisterTypesWhoseNameEndsWith("Converter", typeof(CourseConverter).Assembly, ServiceLifetime.Singleton);

            //register converters in business project
            services.RegisterTypesWhoseNameEndsWith("Converter", typeof(AssignmentWitResultsConverter).Assembly, ServiceLifetime.Singleton);

            //register services
            services.RegisterTypesWhoseNameEndsWith("Service", typeof(CourseService).Assembly, ServiceLifetime.Scoped);

            //register repositories
            services.RegisterTypesWhoseNameEndsWith("Repository", typeof(CourseDbRepository).Assembly, ServiceLifetime.Scoped);

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

        public static void UseDeveloperExceptionJsonResponse(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(
                options =>
                {
                    options.Run(
                        async context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context.Response.ContentType = "application/json";
                            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                            //Place a breakpoint here to inspect the unhandled exception...
                            if (exception != null)
                            {
                                var json = JsonConvert.SerializeObject(exception);
                                await context.Response.WriteAsync(json).ConfigureAwait(false);
                            }
                        });
                }
            );
        }

        private static void RegisterTypesWhoseNameEndsWith(this IServiceCollection services, string classAndInterfaceNameEndsWith,
            Assembly targetAssembly,
            ServiceLifetime lifetime)
        {
            var registrations = from type in targetAssembly.GetExportedTypes()
                where type.Name.EndsWith(classAndInterfaceNameEndsWith) && type.GetInterfaces().Any() && !type.IsInterface
                select new { ServiceType = type.GetInterfaces().First(i => i.Name.EndsWith(classAndInterfaceNameEndsWith)), ImplementationType = type };

            foreach (var registration in registrations)
            {
                services.Add(new ServiceDescriptor(registration.ServiceType, registration.ImplementationType, lifetime));
            }
        }
    }
}

