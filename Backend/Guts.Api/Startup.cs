using Guts.Api.Extensions;
using Guts.Data;
using Guts.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Text;
using Guts.Api.Filters;
using Microsoft.Extensions.Hosting;

namespace Guts.Api
{
    public class Startup
    {
        private readonly Container _container;
        private IWebHostEnvironment _currentHostingEnvironment;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _container = new Container();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddCors();

            services.AddSimpleInjector(_container);

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
                    .UseSqlServer(Configuration.GetConnectionString("GutsDatabase"), sqlOptions =>
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

            services.AddMvc(options =>
                {
                    if (!_currentHostingEnvironment.IsProduction())
                    {
                        options.SslPort = 44318;
                    }

                    options.Filters.Add<LogExceptionFilterAttribute>();

                    options.EnableEndpointRouting = false;

                }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSwaggerDocument(config =>
            {
                config.PostProcess = document =>
                {
                    document.SecurityDefinitions.Add("bearer", new OpenApiSecurityScheme
                    {
                        Type = OpenApiSecuritySchemeType.ApiKey,
                        Name = "Authorization",
                        Description =
                            "Copy 'Bearer ' + valid JWT token into field. You can retrieve a JWT token via '/api/Auth/token'",
                        In = OpenApiSecurityApiKeyLocation.Header
                    });
                    document.Schemes = new List<OpenApiSchema> { OpenApiSchema.Https };
                    document.Info.Title = "GUTS Api";
                    document.Info.Description =
                        "Service that collects and queries data about runs of automated tests in programming exercises.";
                };
            });

            services.AddMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            _currentHostingEnvironment = env;

            if (env.IsDevelopment())
            {
                //  app.UseDeveloperExceptionPage();
                app.UseDeveloperExceptionJsonResponse();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            //  app.UseHttpsRedirection();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseStaticFiles();

            app.UseCookiePolicy();

            app.UseSwagger();
            app.UseSwaggerUi3();

            app.UseCors(builder =>
            {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.SetPreflightMaxAge(TimeSpan.FromHours(1));
                if (env.IsDevelopment())
                {
                    builder.AllowAnyOrigin();
                    //builder.WithOrigins("https://localhost:44310/", "http://localhost:64042");
                }
                else
                {
                    builder.WithOrigins("https://guts-web.pxl.be", "http://guts-web.pxl.be");
                }
            });

            app.UseSimpleInjector(_container, Configuration);

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
