using System;
using System.Linq;
using System.Net;
using System.Reflection;
using Guts.Api.Models.Converters;
using Guts.Bootstrapper;
using Guts.Business.Captcha;
using Guts.Business.Communication;
using Guts.Business.Converters;
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
            Assembly apiAssembly = typeof(CourseConverter).Assembly;

            //register converters in api project
            services.RegisterTypesWhoseNameEndsWith("Converter", apiAssembly, ServiceLifetime.Singleton);

            services.AddDomainLayerServices();
            services.AddBusinessLayerServices(configuration);
            services.AddInfrastructureLayerServices(configuration);
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
                            Exception exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
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

