using System.Linq;
using System.Net;
using System.Reflection;
using Guts.Api.Models.Converters;
using Guts.Business.Captcha;
using Guts.Business.Communication;
using Guts.Business.Converters;
using Guts.Business.Security;
using Guts.Business.Services;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleInjector;
using SimpleInjector.Integration.AspNetCore.Mvc;
using SimpleInjector.Lifestyles;

namespace Guts.Api.Extensions
{
    public static class StartUpExtensions
    {
        public static void AddSimpleInjector(this IServiceCollection services, Container container)
        {
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<IControllerActivator>(
                new SimpleInjectorControllerActivator(container));
            services.AddSingleton<IViewComponentActivator>(
                new SimpleInjectorViewComponentActivator(container));

            services.EnableSimpleInjectorCrossWiring(container);
            services.UseSimpleInjectorAspNetRequestScoping(container);
        }

        public static void UseSimpleInjector(this IApplicationBuilder app, Container container, IConfiguration configuration)
        {
            // Add application presentation components
            container.RegisterMvcControllers(app);
            container.RegisterMvcViewComponents(app);

            //register converters
            RegisterTypesWhoseNameEndsWith("Converter", container, typeof(CourseConverter).Assembly, Lifestyle.Singleton);

            //register services
            RegisterTypesWhoseNameEndsWith("Service", container, typeof(CourseService).Assembly, Lifestyle.Scoped);

            //register repositories
            RegisterTypesWhoseNameEndsWith("DbRepository", container, typeof(CourseDbRepository).Assembly, Lifestyle.Scoped);


            container.Register<IHttpClient, HttpClientAdapter>(Lifestyle.Scoped);
            container.Register<ICaptchaValidator>(() =>
            {
                var captchaSection = configuration.GetSection("Captcha");
                var secret = captchaSection.GetValue<string>("secret");
                var validationUrl = captchaSection.GetValue<string>("validationUrl");
                return new GoogleCaptchaValidator(validationUrl, secret, container.GetInstance<IHttpClient>());
            }, Lifestyle.Singleton);

            container.Register<ISmtpClient>(() =>
            {
                var mailSection = configuration.GetSection("Mail");
                var smtpHost = mailSection.GetValue<string>("host");
                var port = mailSection.GetValue<int>("port");
                var fromEmail = mailSection.GetValue<string>("from");
                var password = mailSection.GetValue<string>("password");
                return new SmtpClientAdapter(smtpHost, port, fromEmail, password);
            });

            container.Register<IMailSender>(() =>
            {
                var mailSection = configuration.GetSection("Mail");
                var fromEmail = mailSection.GetValue<string>("from");
                var webAppBaseUrl = mailSection.GetValue<string>("webappbaseurl");
                return new MailSender(container.GetInstance<ISmtpClient>(), fromEmail, webAppBaseUrl);
            }, Lifestyle.Scoped);

            container.Register<ITokenAccessPassFactory>(() =>
            {
                var tokenSection = configuration.GetSection("Tokens");
                var key = tokenSection.GetValue<string>("Key");
                var issuer = tokenSection.GetValue<string>("Issuer");
                var audience = tokenSection.GetValue<string>("Audience");
                var expirationTimeInMinutes = tokenSection.GetValue<int>("ExpirationTimeInMinutes");

                return new JwtSecurityTokenAccessPassFactory(key, issuer, audience, expirationTimeInMinutes);
            }, Lifestyle.Singleton);

            container.Register<IConfiguration>(() => configuration, Lifestyle.Singleton);

            // Cross-wire ASP.NET services.
            container.CrossWire<UserManager<User>>(app);
            container.CrossWire<RoleManager<Role>>(app);
            container.CrossWire<SignInManager<User>>(app);
            container.CrossWire<RoleManager<User>>(app);
            container.CrossWire<IPasswordHasher<User>>(app);
            container.CrossWire<ILoggerFactory>(app);
            container.CrossWire<GutsContext>(app);
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

        private static void RegisterTypesWhoseNameEndsWith(string classNameEndsWith,
            Container container,
            Assembly targetAssembly,
            Lifestyle lifestyle)
        {
            var registrations = from type in targetAssembly.GetExportedTypes()
                where type.Name.EndsWith(classNameEndsWith) && type.GetInterfaces().Any()
                select new { ServiceType = type.GetInterfaces().First(), ImplementationType = type };

            foreach (var registration in registrations)
            {
                container.Register(registration.ServiceType, registration.ImplementationType, lifestyle);
            }
        }
    }
}

