# Technical Design: Consolidated Scheduled Jobs (Quartz.NET) & Admin Dashboard

- **Feature ID**: `009-scheduled-jobs`
- **Specification**: [`spec.md`](./spec.md)
- **Status**: `Implemented`

---

## 1. Architectural Overview

Recurring time-based background jobs are consolidated into **Quartz.NET** hosted inside `Guts.Api` with a **SQL Server persistent, clustered job store** stored in a dedicated `quartz` schema provisioned via an EF Core migration in `Guts.Infrastructure`. All recurring jobs remain razor-thin adapters (`IRecurringJob`) that delegate directly to existing `Guts.Application` services—preserving strict DDD boundaries (`Guts.Api` never references `Guts.Infrastructure`, and `Guts.Domain` remains pure).

Administrators access the official built-in Blazor **`Quartz.Dashboard`** (`/quartz`) from the Angular Admin sidebar via a secure, single-use ticket ➔ `HttpOnly` cookie bridge (`ScheduledJobsController`). Meanwhile, `StreakReminderService` adopts the existing `MessageTracker` aggregate so all three migrated recurring jobs are idempotent and safe for bounded automatic retries.

```mermaid
flowchart TD
    SPA["Angular SPA (SidebarMenuComponent)"] -->|POST /api/scheduled-jobs/dashboard-ticket (JWT)| SJC["ScheduledJobsController (Guts.Api)"]
    SJC -->|Store 60s single-use ticket| TS["MemoryQuartzDashboardTicketStore (IMemoryCache)"]
    SPA -.->|window.open /api/scheduled-jobs/dashboard-login?ticket=...| SJC
    SJC -->|Consume ticket & SignIn HttpOnly cookie -> 302 /quartz| QD["Quartz.Dashboard (/quartz, Blazor + SignalR)"]

    SYNC["ScheduledJobsSyncHostedService"] -->|Reconcile IRecurringJob + appsettings| QSCHED["Quartz IScheduler (Clustered SQL Store)"]
    QD -->|Inspect / Trigger / Pause / Resume| QSCHED
    QSCHED <-->|quartz.QRTZ_* tables| SQL["Guts SQL Server DB (quartz schema)"]

    QSCHED --> LISTENER["RecurringJobListener (Logging + Bounded Retries)"]
    QSCHED --> SLJ["SprintLeaderboardJob"]
    QSCHED --> SRJ["StreakReminderJob"]
    QSCHED --> PARJ["PeerAssessmentReminderJob"]

    SLJ --> GS["IGamificationService"]
    SRJ --> SRS["IStreakReminderService"]
    PARJ --> PARS["IPeerAssessmentReminderService"]

    SRS --> MTR["IMessageTrackerRepository"]
    SRS --> MTF["IMessageTrackerFactory"]
    PARS --> MTR
    PARS --> MTF
    MTF --> MT["MessageTracker (Guts.Domain)"]
```

---

## 2. Domain Layer (`Guts.Domain`)

No new aggregates or domain types are needed. Existing domain primitives are reused directly:

- **`IsoWeek` (`Guts.Domain/ValueObjects/IsoWeek.cs`)** (`[DR-3]`):
  - Used by `SprintLeaderboardJob` to compute the previous completed ISO week from the job's `Europe/Brussels` local fire time (`IsoWeek.From(localFireTime).Previous()`).
- **`MessageTracker` & `IMessageTrackerFactory` (`Guts.Domain/MessageTrackerAggregate/`)** (`[AC-4]`, `[DR-4]`):
  - Reused by `StreakReminderService` with `EntityType = $"StreakReminder:{currentWeek.Year}-W{currentWeek.WeekNumber:D2}"` (23 chars, well under `MaxEntityTypeLength = 64`), `EntityId = course.Id`, and `Email = Email.Create(user.Email)`.
  - Enforces existing state invariants (`Sending` ➔ `Succeeded` / `Failed`, `CanRetry` when `Status == Failed && RetryCount < 3`, `Retry(nowUtc)`).

---

## 3. Application Layer (`Guts.Application`) — Use-Case Orchestration Only

> [!IMPORTANT]
> Zero domain logic in `Guts.Application`. `StreakReminderService` delegates all tracker state transitions and retry eligibility to `MessageTracker` and `IMessageTrackerFactory`.

### Application Services (`Guts.Application/Services/`)
- **`StreakReminderService` (`Guts.Application/Services/StreakReminderService.cs`)** (`[AC-4]`, `[DR-4]`):
  - Add dependencies `IMessageTrackerRepository` and `IMessageTrackerFactory` to the primary constructor (matching `PeerAssessmentReminderService`).
  - Consolidate/update constructor overloads so DI auto-registration unambiguously resolves the full constructor (keeping a single clean constructor with all DI-registered dependencies; unit tests can pass mocks directly).
  - Update `SendMessagesAsync(List<StreakReminderEmailMessage> messagesToSend, IsoWeek currentWeek, DateTime executionTimeUtc, CancellationToken cancellationToken)`:
    1. Build `string entityType = $"StreakReminder:{currentWeek.Year}-W{currentWeek.WeekNumber:D2}";`.
    2. For each message, construct `Email recipientEmail = Email.Create(message.Recipient.Email);` (skipping if invalid/empty).
    3. Look up existing tracker via `_messageTrackerRepository.FindByEntityAndEmailAsync(entityType, message.CourseId, recipientEmail)`:
       - If `tracker == null`: create via `_messageTrackerFactory.CreateNew(entityType, message.CourseId, recipientEmail, executionTimeUtc)` and persist via `_messageTrackerRepository.AddAsync(tracker)`.
       - Else if `tracker.CanRetry`: call `tracker.Retry(executionTimeUtc)` and persist via `_messageTrackerRepository.UpdateAsync(tracker)`.
       - Else (`Status == Succeeded`, `Status == Sending`, or `RetryCount >= 3`): skip sending and continue to next message.
    4. Call `_mailSender.SendStreakReminderEmailAsync(...)`:
       - On success: `tracker.MarkSucceeded(); await _messageTrackerRepository.UpdateAsync(tracker); sentCount++;`.
       - On exception: `tracker.MarkFailed(ex.Message); await _messageTrackerRepository.UpdateAsync(tracker);` and log error (without aborting remaining messages).
    5. Apply `_mailThrottler.ThrottleAsync(cancellationToken)` between actual send attempts.

---

## 4. Infrastructure Layer (`Guts.Infrastructure`)

### EF Core Configuration & Migrations
- **`GutsContext`**: No new `DbSet<>` properties or entity configurations needed (Quartz manages `quartz.QRTZ_*` directly via `Microsoft.Data.SqlClient`).
- **Migration Required?**: `Yes`
  - Migration name: `AddQuartzSchema` (`Backoffice/Guts.Infrastructure/Migrations/<timestamp>_AddQuartzSchema.cs`).
  - `Up(MigrationBuilder)`:
    - Creates schema `[quartz]` if it does not exist (`IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'quartz') EXEC(N'CREATE SCHEMA [quartz]');`).
    - Executes the official Quartz.NET SQL Server DDL script targeting `[quartz].[QRTZ_*]` tables (`QRTZ_CALENDARS`, `QRTZ_CRON_TRIGGERS`, `QRTZ_BLOB_TRIGGERS`, `QRTZ_FIRED_TRIGGERS`, `QRTZ_PAUSED_TRIGGER_GRPS`, `QRTZ_SCHEDULER_STATE`, `QRTZ_LOCKS`, `QRTZ_JOB_DETAILS`, `QRTZ_SIMPLE_TRIGGERS`, `QRTZ_SIMPROP_TRIGGERS`, `QRTZ_TRIGGERS`) plus primary keys, foreign keys, and indexes.
  - `Down(MigrationBuilder)`:
    - Drops the `[quartz].[QRTZ_*]` tables in FK-safe order and drops schema `[quartz]`.
  - Runs automatically on startup via `GutsDbInitializer.DoAutomaticMigrations()`.

---

## 5. API Layer (`Guts.Api`)

### NuGet Packages & Project Configuration (`Guts.Api/Guts.Api.csproj`)
- Add packages:
  - `Quartz`
  - `Quartz.Extensions.Hosting`
  - `Quartz.Serialization.SystemTextJson`
  - `Quartz.Dashboard`
- Add `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>` in `<PropertyGroup>` so Blazor static web assets for `Quartz.Dashboard` resolve cleanly in a pure ASP.NET Core API project on .NET 10.

### Scheduled Jobs Framework (`Guts.Api/ScheduledJobs/`)
- **`MisfirePolicy`** (`MisfirePolicy.cs`):
  - `FireAndProceed` (maps to `WithMisfireHandlingInstructionFireAndProceed()`)
  - `DoNothing` (maps to `WithMisfireHandlingInstructionDoNothing()`)
- **`IRecurringJob`** (`IRecurringJob.cs`):
  ```csharp
  public interface IRecurringJob : IJob
  {
      static abstract string JobName { get; }
      static abstract string Description { get; }
      static abstract string DefaultCronExpression { get; }
      static abstract MisfirePolicy MisfirePolicy { get; }
      static abstract int MaxRetries { get; }
      static abstract TimeSpan RetryDelay { get; }
  }
  ```
  - Static abstract metadata keeps job discovery clean, strongly typed, and zero-instantiation during startup sync, while also allowing a small `RecurringJobDescriptor` record for easy unit testing and DI registration.
- **`ScheduledJobsOptions` & `ScheduledJobConfig`** (`ScheduledJobsOptions.cs`):
  - `public const string SectionName = "ScheduledJobs";`
  - `public string DefaultTimeZoneId { get; set; } = "Europe/Brussels";`
  - `public Dictionary<string, ScheduledJobConfig> Jobs { get; set; } = new(StringComparer.OrdinalIgnoreCase);`
  - `ScheduledJobConfig`: `bool Enabled = true`, `string CronExpression = null`, `string TimeZoneId = null`.
- **Migrated Jobs (`Guts.Api/ScheduledJobs/Jobs/`)** (`[AC-3]`):
  1. **`SprintLeaderboardJob`** (`[DisallowConcurrentExecution]`):
     - `JobName = "SprintLeaderboard"`, `DefaultCronExpression = "0 1 0 ? * MON"`, `MisfirePolicy = FireAndProceed`, `MaxRetries = 3`, `RetryDelay = TimeSpan.FromHours(1)`.
     - Resolves the trigger's timezone (or `Europe/Brussels` fallback), converts `context.FireTimeUtc` to local time, computes `IsoWeek.From(localFireTime).Previous()`, and calls `IGamificationService.AwardWeeklySprintBadgesAsync(prevWeek.Year, prevWeek.WeekNumber)`.
  2. **`StreakReminderJob`** (`[DisallowConcurrentExecution]`):
     - `JobName = "StreakReminder"`, `DefaultCronExpression = "0 0 12 ? * THU"`, `MisfirePolicy = FireAndProceed`, `MaxRetries = 3`, `RetryDelay = TimeSpan.FromMinutes(15)`.
     - Calls `IStreakReminderService.SendStreakRemindersAsync(context.FireTimeUtc.UtcDateTime, context.CancellationToken)`.
  3. **`PeerAssessmentReminderJob`** (`[DisallowConcurrentExecution]`):
     - `JobName = "PeerAssessmentReminder"`, `DefaultCronExpression = "0 0 */2 ? * *"`, `MisfirePolicy = DoNothing`, `MaxRetries = 3`, `RetryDelay = TimeSpan.FromMinutes(15)`.
     - Calls `IPeerAssessmentReminderService.SendAssessmentRemindersAsync(context.FireTimeUtc.UtcDateTime, context.CancellationToken)`.
- **`RecurringJobListener : IJobListener`** (`Guts.Api/ScheduledJobs/RecurringJobListener.cs`) (`[AC-6]`):
  - `JobToBeExecuted`: logs structured start (`JobName`, `FireInstanceId`, `RetryAttempt`).
  - `JobWasExecuted`:
    - On success: logs completion with `context.JobRunTime`.
    - On `jobException != null`:
      - Unwraps `JobExecutionException` / checks cancellation; logs structured error.
      - Reads `MaxRetries` and `RetryDelaySeconds` from `JobDetail.JobDataMap` and current `RetryAttempt` from `MergedJobDataMap`.
      - If `!context.CancellationToken.IsCancellationRequested && retryAttempt < maxRetries`: schedules a one-off `TriggerBuilder` firing at `DateTimeOffset.UtcNow.Add(retryDelay)` with `RetryAttempt = retryAttempt + 1` in `JobDataMap`, targeting the same `JobKey`.
      - Otherwise logs final failure (retry budget exhausted or retries disabled).
- **`ScheduledJobsSyncHostedService : IHostedService`** (`Guts.Api/ScheduledJobs/ScheduledJobsSyncHostedService.cs`) (`[AC-2]`, `[DR-1]`):
  - Receives `ISchedulerFactory`, `IEnumerable<RecurringJobDescriptor>`, `IOptions<ScheduledJobsOptions>`, and `ILogger`.
  - On `StartAsync`:
    - Validates all `JobName`s are unique and non-empty, resolves `TimeZoneInfo`, and validates every effective cron expression (`CronExpression.IsValidExpression`). Fails fast on invalid config.
    - For each descriptor:
      - Upserts durable `IJobDetail` (`StoreDurably()`, `RequestRecovery()`, `MaxRetries` & `RetryDelaySeconds` in `JobDataMap`).
      - Builds the recurring `ICronTrigger` (`{JobName}-trigger`) with effective cron expression, `TimeZoneInfo`, and configured `MisfirePolicy`.
      - Checks existing trigger in Quartz DB: creates it if missing, or reschedules it (`RescheduleJob`) if cron expression, timezone, or misfire instruction changed.
      - If `Enabled == false`: pauses the job/trigger (`PauseJob`). If `Enabled == true` and previously paused by config: resumes (`ResumeJob`).
- **Removed Legacy Services**:
  - Delete `SprintLeaderboardBackgroundService.cs`, `StreakReminderBackgroundService.cs`, `PeerAssessmentReminderBackgroundService.cs` and their corresponding test files in `Guts.Api.Tests/BackgroundServices/`.
  - Keep `DeletionBackgroundService.cs` and `InMemoryDeletionJobQueue.cs` untouched.

### Dashboard Authentication Bridge & Controller (`Guts.Api/ScheduledJobs/` & `Guts.Api/Controllers/`)
- **Constants (`ApiConstants.cs`)**:
  - `QuartzDashboardCookieScheme = "QuartzDashboardCookie"`
  - `QuartzDashboardAdminPolicy = "QuartzDashboardAdminPolicy"`
- **`IQuartzDashboardTicketStore` / `MemoryQuartzDashboardTicketStore`** (`Guts.Api/ScheduledJobs/QuartzDashboardTicketStore.cs`) (`[AC-7]`):
  - `string CreateTicket(ClaimsPrincipal adminPrincipal)`: generates 32 cryptographically random bytes (`RandomNumberGenerator.GetBytes(32)`), Base64Url-encodes them, stores a lightweight `QuartzDashboardTicket` (user ID, username, role claims) in `IMemoryCache` with 60-second absolute expiration.
  - `ClaimsPrincipal ConsumeTicket(string ticket)`: uses a lock to atomically check & remove the ticket from `IMemoryCache`, returning a `ClaimsPrincipal` under `QuartzDashboardCookieScheme` (or `null` if missing/expired/already used).
- **`ScheduledJobsController`** (`Guts.Api/Controllers/ScheduledJobsController.cs`) (`[AC-7]`):
  - Route: `[Route("api/scheduled-jobs")]`, inherits `ControllerBase`.
  - `POST dashboard-ticket` (`[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]`):
    - Blocked during impersonation automatically by `ImpersonationReadOnlyFilter`.
    - Calls `_ticketStore.CreateTicket(User)`.
    - Returns `200 OK` with `QuartzDashboardTicketModel { LoginUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/scheduled-jobs/dashboard-login?ticket={Uri.EscapeDataString(ticket)}" }`.
  - `GET dashboard-login` (`[AllowAnonymous]`):
    - Accepts `[FromQuery] string ticket`.
    - Calls `_ticketStore.ConsumeTicket(ticket)`. If `null`, returns `401 Unauthorized`.
    - Calls `await HttpContext.SignInAsync(ApiConstants.QuartzDashboardCookieScheme, principal, new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) })`.
    - Returns `Redirect($"{GetNormalizedPathBase()}/quartz")` so sub-path deployments (e.g. `/staging`) redirect to `/staging/quartz` instead of root `/quartz`.

### Startup & Pipeline Registration (`Guts.Api/Startup.cs` & `ScheduledJobsServiceCollectionExtensions.cs`)
- **Authentication & Authorization**:
  - Add `.AddCookie(ApiConstants.QuartzDashboardCookieScheme, options => { options.Cookie.Name = ApiConstants.GetQuartzDashboardCookieName(Environment.EnvironmentName); options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; options.Cookie.SameSite = SameSiteMode.Lax; options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; options.ExpireTimeSpan = TimeSpan.FromMinutes(30); options.SlidingExpiration = true; options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }; options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }; })` so Production (`/`) and Staging (`/staging`) on `guts-api.pxl.be` never collide on cookie name.
  - Add policy `ApiConstants.QuartzDashboardAdminPolicy` requiring `QuartzDashboardCookieScheme`, `RequireAuthenticatedUser()`, and `RequireRole(Role.Constants.Admin)`.
- **Quartz Registration (`AddScheduledJobs`)**:
  - Configures `AddQuartz(q => { q.SchedulerId = "AUTO"; q.SchedulerName = "GutsBackofficeScheduler"; q.AddJobListener<RecurringJobListener>(); q.UsePersistentStore(s => { s.UseProperties = true; s.UseSqlServer(sql => { sql.ConnectionString = connectionString; sql.TablePrefix = "[quartz].[QRTZ_]"; }); s.UseSystemTextJsonSerializer(); s.UseClustering(); }); q.AddHttpApi(...); })` (if required by installed `Quartz.Dashboard`).
  - Registers `AddQuartzHostedService(o => o.WaitForJobsToComplete = true)`, `AddQuartzDashboard(o => { o.AuthorizationPolicy = ApiConstants.QuartzDashboardAdminPolicy; })`, `IQuartzDashboardTicketStore`, auto-discovered `RecurringJobDescriptor`s, and `ScheduledJobsSyncHostedService`.
- **Middleware (`Startup.Configure`)**:
  - Add `app.UseAntiforgery();` after `app.UseAuthorization();`.
  - Inside `app.UseEndpoints(...)`: map `endpoints.MapQuartzDashboard().RequireAuthorization(ApiConstants.QuartzDashboardAdminPolicy);` (and `MapQuartzApi().RequireAuthorization(ApiConstants.QuartzDashboardAdminPolicy)` if required by the package).
- **Configuration (`appsettings.json`)**:
  - Replace `StreakReminders` and `PeerAssessmentReminders` sections with the unified `ScheduledJobs` section (`[AC-5]`).

---

## 6. Frontend Layer (`Guts.Web/ClientApp`)

### Services (`src/app/services/`)
- **`ScheduledJobsService`** (`src/app/services/scheduled-jobs.service.ts`):
  - Injectable service (`providedIn: 'root'`) calling `POST ${apiUrl}/api/scheduled-jobs/dashboard-ticket`.
  - Returns `Observable<GetResult<IQuartzDashboardTicketModel>>` where `IQuartzDashboardTicketModel = { loginUrl: string }`.

### Components (`src/app/components/sidebar-menu/`)
- **`SidebarMenuComponent`** (`sidebar-menu.component.ts` & `.html`):
  - Injects `ScheduledJobsService` and `ToastrService` (or existing notification service used in the app).
  - Under `@if (userProfile.isAdmin() && !isImpersonating)` in the Admin menu, adds a **"Scheduled Jobs"** item with external-link icon (`bi-box-arrow-up-right`).
  - Clicking invokes `openScheduledJobsDashboard()`:
    - Calls `scheduledJobsService.createDashboardTicket()`.
    - On success: opens `result.value.loginUrl` in a new tab via `window.open(result.value.loginUrl, '_blank', 'noopener,noreferrer')`.
    - On failure: displays an error toast/notification.

---

## 7. Dependency Injection (`Guts.Bootstrapper`) & Architecture Compliance

- **`Guts.Application`**: `StreakReminderService` remains auto-registered as Scoped (`*Service` convention in `StartUpExtensions.cs`); its new `IMessageTrackerRepository` and `IMessageTrackerFactory` dependencies are already auto-registered.
- **`Guts.Api`**:
  - `AddScheduledJobs(Configuration)` is called from `Startup.ConfigureServices`.
  - `Guts.Api` does **not** reference `Guts.Infrastructure` (passes `ApiLayer_ShouldNotDependDirectlyOnInfrastructureLayer` in `ArchitectureTests.cs`).
  - `ScheduledJobsController` resides in `Guts.Api.Controllers`, ends with `Controller`, and explicitly declares `[Authorize]` / `[AllowAnonymous]` on its actions (passes `ApiControllers_ShouldBeSecuredAndResideInControllersNamespace`).
