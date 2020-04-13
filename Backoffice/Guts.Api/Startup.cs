﻿using Guts.Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
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
using Microsoft.Extensions.Hosting;
using NSwag.Generation.Processors.Security;

namespace Guts.Api
{
    public class Startup
    {
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
                options.Filters.Add<LogExceptionFilterAttribute>();
                options.Filters.Add<LogBadRequestFilterAttribute>();
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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionJsonResponse();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

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
