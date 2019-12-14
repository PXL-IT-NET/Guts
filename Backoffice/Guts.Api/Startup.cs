using Guts.Api.Extensions;
using Guts.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using Guts.Api.Controllers;
using Guts.Api.Filters;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.Extensions.Hosting;
using NSwag.Generation.Processors.Security;

namespace Guts.Api
{
    public class Startup
    {
        private IWebHostEnvironment _currentHostingEnvironment;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
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

            services.AddControllers(options =>
            {
                if (!_currentHostingEnvironment.IsProduction())
                {
                    options.SslPort = 44318;
                }

                options.Filters.Add<LogExceptionFilterAttribute>();
            });

            services.AddOpenApiDocument(document =>
            {
                document.AddSecurity("bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    Description =
                        "Copy 'Bearer ' + valid JWT token into field. You can retrieve a JWT token via '/api/Auth/token'",
                    In = OpenApiSecurityApiKeyLocation.Header
                });
                document.PostProcess = document =>
                {
                    document.Schemes = new List<OpenApiSchema> { OpenApiSchema.Https };
                    document.Info.Title = "GUTS Api";
                    document.Info.Description =
                        "Service that collects and queries data about runs of automated tests in programming exercises.";
                };
                document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));
            });

            services.AddMemoryCache();

            var apiAssembly = typeof(CourseController).Assembly;
            services.AddAutoMapper(apiAssembly);

            services.AddApplicationServices(Configuration);
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

            app.UseRouting();

            app.UseCookiePolicy();

            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseCors(builder =>
            {
                builder.AllowAnyHeader();
                builder.WithExposedHeaders("Content-Disposition");
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

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
