using System;
using System.Reflection;
using System.Text;
using Guts.Api.Extensions;
using Guts.Data;
using Guts.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NJsonSchema;
using NSwag;
using NSwag.AspNetCore;
using NSwag.SwaggerGeneration.Processors.Security;
using SimpleInjector;

namespace Guts.Api
{
    public class Startup
    {
        private const string GutsOriginsPolicy = "GutsOriginsPolicy";

        private readonly Container _container;
        private IHostingEnvironment _currentHostingEnvironment;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _container = new Container();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(GutsOriginsPolicy, builder =>
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            services.AddSimpleInjector(_container);

            services.AddDbContext<GutsContext>(options =>
            {
                options.UseMySql(Configuration.GetConnectionString("GutsDatabaseMySql"), sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("Guts.Data");
                });
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

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = Configuration["Tokens:Issuer"],
                        ValidAudience = Configuration["Tokens:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"])),

                    };
                });

            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConfiguration(Configuration.GetSection("Logging"));
                builder.AddEventSourceLogger();
            });

            services.AddMvc(options =>
            {
                if (!_currentHostingEnvironment.IsProduction())
                {
                    options.SslPort = 44318;
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            _currentHostingEnvironment = env;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseStaticFiles();

            // Enable the Swagger UI middleware and the Swagger generator
            app.UseSwaggerUi3(typeof(Startup).GetTypeInfo().Assembly, settings =>
            {
                settings.SwaggerRoute = "/swagger";
                settings.SwaggerUiRoute = "/docs";
                settings.GeneratorSettings.IsAspNetCore = true;
                settings.GeneratorSettings.DefaultPropertyNameHandling = PropertyNameHandling.CamelCase;

                settings.PostProcess = document =>
                {
                    document.Info.Title = "GUTS Api";
                    document.Info.Description =
                        "Service that collects and queries data about runs of automated tests in programming exercises.";

                };

                settings.GeneratorSettings.OperationProcessors.Add(new OperationSecurityScopeProcessor("Bearer Token"));
                settings.GeneratorSettings.DocumentProcessors.Add(new SecurityDefinitionAppender("Bearer Token",
                    new SwaggerSecurityScheme
                    {
                        Type = SwaggerSecuritySchemeType.ApiKey,
                        Name = "Authorization",
                        Description = "Copy 'Bearer ' + valid JWT token into field. You can retrieve a JWT token via '/api/Auth/token'",
                        In = SwaggerSecurityApiKeyLocation.Header
                    }));
            });

            app.UseCors(GutsOriginsPolicy);

            app.UseSimpleInjector(_container, Configuration);

            app.UseAuthentication();

            app.UseMvc();

            app.DoAutomaticMigrations();
        }
    }
}
