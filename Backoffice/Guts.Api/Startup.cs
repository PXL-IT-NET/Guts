using Guts.Api.BackgroundServices;
using Guts.Api.Extensions;
using Guts.Api.ScheduledJobs;
using Guts.Application.Services.Deletion;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Guts.Api.Filters;
using Guts.Api.Models;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Hosting;
using NSwag.Generation.Processors.Security;
using Quartz;

namespace Guts.Api;

[ExcludeFromCodeCoverage]
public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public IConfiguration Configuration { get; } = configuration;
    public IWebHostEnvironment Environment { get; } = environment;

    // This method gets called by the runtime. Use this method to add services to the container.
    [SuppressMessage("Security", "S6781:JWT secret keys should not be disclosed", Justification = "Local development docker-compose override uses a dummy test secret; production and staging use environment variables.")]
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
#pragma warning disable S6781 // JWT secret keys should not be disclosed
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"])),
#pragma warning restore S6781
                };
            })
            .AddCookie(ApiConstants.QuartzDashboardCookieScheme, options =>
            {
                options.Cookie.Name = ApiConstants.GetQuartzDashboardCookieName(Environment.EnvironmentName);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization(options =>
        {
            var authenticatedUsersOnlyPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(ApiConstants.AuthenticatedUsersOnlyPolicy, authenticatedUsersOnlyPolicy);
            options.DefaultPolicy = authenticatedUsersOnlyPolicy;

            var lectorsOnlyPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireRole(Role.Constants.Lector)
                .Build();
            options.AddPolicy(ApiConstants.LectorsOnlyPolicy, lectorsOnlyPolicy);

            var adminOnlyPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireRole(Role.Constants.Admin)
                .Build();
            options.AddPolicy(ApiConstants.AdminOnlyPolicy, adminOnlyPolicy);

            var lectorOrAdminPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireRole(Role.Constants.Admin, Role.Constants.Lector)
                .Build();
            options.AddPolicy(ApiConstants.LectorOrAdminPolicy, lectorOrAdminPolicy);

            var quartzDashboardAdminPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(ApiConstants.QuartzDashboardCookieScheme)
                .RequireAuthenticatedUser()
                .RequireRole(Role.Constants.Admin)
                .Build();
            options.AddPolicy(ApiConstants.QuartzDashboardAdminPolicy, quartzDashboardAdminPolicy);
        });

        services.AddControllers(options =>
        {
            options.Filters.Add(new AuthorizeFilter(ApiConstants.AuthenticatedUsersOnlyPolicy));
            options.Filters.Add<ImpersonationReadOnlyFilter>();
            options.Filters.Add<ApplicationExceptionFilterAttribute>();
            options.Filters.Add<LogBadRequestFilterAttribute>();
        });

        services.AddOpenApiDocument(document =>
        {
            document.AddSecurity("bearer", [], new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                Description =
                    "Copy 'Bearer ' + valid JWT token into field. You can retrieve a JWT token via '/api/Auth/token'",
                In = OpenApiSecurityApiKeyLocation.Header
            });
            document.PostProcess = document =>
            {
                document.Schemes = [OpenApiSchema.Https];
                document.Info.Title = "GUTS Api";
                document.Info.Description =
                    "Service that collects and queries data about runs of automated tests in programming exercises.";
            };
            document.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));
        });

        services.AddMemoryCache();

        services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.Stateless = true;
            })
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly();

        services.AddSingleton<IMapper, Mapper>();

        services.AddApplicationServices(Configuration);
        services.AddScheduledJobs(Configuration);
        services.AddSingleton<IDeletionJobQueue, InMemoryDeletionJobQueue>();
        services.AddHostedService<DeletionBackgroundService>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionJsonResponse();
        }
        else
        {
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
        app.UseSwaggerUi();

        app.UseCors(builder =>
        {
            builder.AllowAnyHeader();
            builder.WithExposedHeaders("Content-Disposition");
            builder.AllowAnyMethod();
            builder.SetPreflightMaxAge(TimeSpan.FromHours(1));

            string[] allowedOrigins = Configuration.GetSection("AllowedOrigins").Get<string[]>();
            builder.WithOrigins(allowedOrigins);

        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapMcp("/mcp");
            endpoints.MapQuartzDashboard().RequireAuthorization(ApiConstants.QuartzDashboardAdminPolicy);
        });
    }
}