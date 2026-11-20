# Specification: Consolidated Scheduled Jobs (Quartz.NET) & Admin Dashboard

- **Feature ID**: `009-scheduled-jobs`
- **Status**: `Implemented`
- **Created**: `2026-11-20`

---

## 1. Context & Problem Statement

GUTS Backoffice currently runs multiple recurring background tasks as independent `Microsoft.Extensions.Hosting.BackgroundService` classes in `Guts.Api/BackgroundServices/`:

- `SprintLeaderboardBackgroundService` (weekly Monday badge awarding & Hall of Fame rank defense)
- `StreakReminderBackgroundService` (weekly Thursday midday streak reminder emails)
- `PeerAssessmentReminderBackgroundService` (2-hourly peer assessment deadline reminder emails)

Each service duplicates its own `Task.Delay` scheduling loop, DI scope creation, configuration parsing, and error handling—with inconsistent recovery guarantees (for example, `SprintLeaderboardBackgroundService` stores last-success state only in memory, while `StreakReminderBackgroundService` has no misfire recovery, no retry, and no per-recipient delivery tracking to prevent duplicate emails on retry). Adding upcoming recurring tasks (such as another weekly job) would further multiply this boilerplate.

This feature:
1. Consolidates all **time-based recurring background tasks** onto **Quartz.NET** with a **SQL Server persistent, clustered job store** managed in a dedicated `quartz` schema via EF Core migrations.
2. Introduces a **convention-based `IRecurringJob` abstraction** for trivial extensibility and a unified `ScheduledJobs` configuration section (defaulting to `Europe/Brussels` time).
3. Makes **`StreakReminderService` idempotent** via the existing `MessageTracker` aggregate (matching `PeerAssessmentReminderService`) so `StreakReminderJob` can safely participate in automatic retries and manual re-triggers without sending duplicate emails.
4. Exposes the official built-in Blazor **`Quartz.Dashboard`** (`/quartz`) strictly to **Administrators** via a short-lived single-use ticket ➔ `HttpOnly` cookie bridge, linked directly from the Angular Admin sidebar.

> **Note**: Event/queue-driven background workers—specifically `DeletionBackgroundService` (`006-chunked-cascade-deletion`), which reacts immediately to `InMemoryDeletionJobQueue` (`Channel<Guid>`) and tracks domain-owned `DeletionJob` progress—remain separate by design.

---

## 2. User Stories & Actors

- **Actors**: `Administrator`, `Background Scheduler (Quartz.NET)`, `Developer / Maintainer`

- **US-1 (Reliable, Recoverable Recurring Execution)**: As an **Administrator**, I want all recurring background tasks to be persisted in SQL Server with clustering and per-job misfire recovery so that server restarts, brief downtime, or rolling deployments never silently skip weekly jobs or cause duplicate concurrent executions.
- **US-2 (Easy Extensibility for New Jobs)**: As a **Developer**, I want to add a new recurring job by implementing a single convention-discovered `IRecurringJob` class with its default cron schedule, misfire policy, and retry policy so that adding future weekly/periodic tasks requires zero `Startup.cs` or scheduler-loop boilerplate.
- **US-3 (Unified Configuration & Local Timezone)**: As an **Administrator**, I want all recurring jobs configured in a single `ScheduledJobs` section of `appsettings.json` (defaulting to `Europe/Brussels` time so DST shifts are respected) with changes automatically synced to Quartz on startup.
- **US-4 (Idempotent Streak Reminder Delivery)**: As a **Student**, I want Thursday streak reminder emails to be tracked per course and ISO week via `MessageTracker` so that automatic retries or manual admin triggers never send me duplicate reminder emails in the same week.
- **US-5 (Admin-Only Quartz Dashboard)**: As an **Administrator**, I want to open the built-in Blazor **Quartz Dashboard** (`/quartz`) directly from the Angular Admin sidebar (authenticated strictly as Admin, without exposing it to Lectors, Students, or anonymous users) so that I can inspect live job/trigger/cluster status, pause/resume jobs, and manually trigger jobs on demand.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Persistent, Clustered Quartz.NET Job Store via EF Core Migration
- **Given** the Backoffice API starts up and runs `GutsDbInitializer.DoAutomaticMigrations()`
- **When** the database migration runs on a fresh or existing GUTS SQL Server database
- **Then** the official Quartz.NET SQL Server tables (`QRTZ_*`) are created inside a dedicated `quartz` database schema via an EF Core migration in `Guts.Infrastructure/Migrations/`
- **And** Quartz.NET is configured with `UsePersistentStore`, `UseSqlServer`, `UseSystemTextJsonSerializer`, `UseClustering()`, and graceful shutdown (`WaitForJobsToComplete = true`).

### AC-2: Convention-Based `IRecurringJob` Discovery & Startup Synchronization
- **Given** one or more concrete classes implementing `IRecurringJob` (which extends `Quartz.IJob`) in `Guts.Api`
- **When** the API host starts
- **Then** all `IRecurringJob` implementations are automatically discovered and registered with Quartz.NET without manual per-job wiring in `Startup.cs`
- **And** every recurring job enforces `[DisallowConcurrentExecution]` so overlapping runs of the same job cannot occur
- **And** startup synchronization reconciles code defaults + `appsettings.json` overrides (`ScheduledJobs:DefaultTimeZoneId`, `ScheduledJobs:Jobs:{JobName}:Enabled`, `CronExpression`, `TimeZoneId`) against the persistent Quartz store:
  - Creating missing jobs and cron triggers.
  - Updating existing triggers when the configured `CronExpression`, `TimeZoneId`, or `MisfirePolicy` changes.
  - Pausing triggers for jobs where `Enabled == false` (and resuming/scheduling them when `Enabled == true`).

### AC-3: Consolidated Existing Recurring Jobs & Timezone Behavior
- **Given** the three existing recurring background services are migrated to `IRecurringJob` implementations and the old `BackgroundService` classes (`SprintLeaderboardBackgroundService`, `StreakReminderBackgroundService`, `PeerAssessmentReminderBackgroundService`) are removed
- **When** Quartz schedules them using the default `Europe/Brussels` timezone:
  1. **`SprintLeaderboard`** (`SprintLeaderboardJob`):
     - Default Cron: `0 1 0 ? * MON` (Mondays at `00:01 Europe/Brussels`).
     - Misfire policy: `FireAndProceed`.
     - Retries: Enabled (`MaxRetries = 3` with backoff).
     - Computes the previous completed ISO week (`IsoWeek.From(localExecutionTime).Previous()`) from the **`Europe/Brussels` local fire time** (avoiding an off-by-one-week error since Monday `00:01` CET/CEST is Sunday `23:01`/`22:01` UTC) and delegates to `IGamificationService.AwardWeeklySprintBadgesAsync(year, weekNumber)`.
  2. **`StreakReminder`** (`StreakReminderJob`):
     - Default Cron: `0 0 12 ? * THU` (Thursdays at `12:00 Europe/Brussels`).
     - Misfire policy: `FireAndProceed`.
     - Retries: Enabled (`MaxRetries = 3` with backoff, made safe by `AC-4`).
     - Delegates to `IStreakReminderService.SendStreakRemindersAsync(utcExecutionTime, cancellationToken)`.
  3. **`PeerAssessmentReminder`** (`PeerAssessmentReminderJob`):
     - Default Cron: `0 0 */2 ? * *` (every 2 hours on the hour).
     - Misfire policy: `DoNothing` (skip missed ticks; wait for the next 2-hour tick).
     - Retries: Enabled (`MaxRetries = 3` with backoff; `PeerAssessmentReminderService` is idempotent via `MessageTracker`).
     - Delegates to `IPeerAssessmentReminderService.SendAssessmentRemindersAsync(utcExecutionTime, cancellationToken)`.
- **And** `DeletionBackgroundService` and `InMemoryDeletionJobQueue` remain untouched and continue running independently.

### AC-4: Idempotent Streak Reminder Delivery via `MessageTracker`
- **Given** `StreakReminderService.SendStreakRemindersAsync(executionTime, cancellationToken)` processes eligible students for a `Course` in ISO week `(year, weekNumber)`
- **When** dispatching a streak reminder email to a student (`Email`)
- **Then** delivery is tracked using the existing `MessageTracker` aggregate with `EntityType = "StreakReminder:{year}-W{weekNumber:D2}"` (within `MaxEntityTypeLength = 64`), `EntityId = course.Id`, and `Email = student.Email`:
  - If a `MessageTracker` already exists with `Status == Succeeded` or `Status == Sending`, or `Status == Failed` with `RetryCount >= 3`, the student is skipped (no duplicate email sent).
  - If no tracker exists, a new `MessageTracker` is created via `IMessageTrackerFactory.CreateNew(...)` in `Sending` status and persisted before calling `IMailSender`.
  - If a tracker exists with `CanRetry == true` (`Status == Failed` and `RetryCount < 3`), `tracker.Retry(nowUtc)` transitions it back to `Sending` and is persisted before calling `IMailSender`.
  - Upon successful send, `tracker.MarkSucceeded()` is called and persisted.
  - Upon send failure, `tracker.MarkFailed(ex.Message)` is called and persisted, the error is logged, and processing continues for remaining eligible students.

### AC-5: Unified `ScheduledJobs` Configuration
- **Given** `Backoffice/Guts.Api/appsettings.json`
- **When** configuring scheduled jobs
- **Then** the legacy `StreakReminders` and `PeerAssessmentReminders` configuration sections are replaced by a single `ScheduledJobs` section:
  ```json
  "ScheduledJobs": {
    "DefaultTimeZoneId": "Europe/Brussels",
    "Jobs": {
      "SprintLeaderboard": {
        "Enabled": true,
        "CronExpression": "0 1 0 ? * MON"
      },
      "StreakReminder": {
        "Enabled": true,
        "CronExpression": "0 0 12 ? * THU"
      },
      "PeerAssessmentReminder": {
        "Enabled": true,
        "CronExpression": "0 0 */2 ? * *"
      }
    }
  }
  ```
- **And** if a job entry is omitted from `appsettings.json`, it defaults to `Enabled = true`, its code-defined `DefaultCronExpression`, and `ScheduledJobs:DefaultTimeZoneId` (`Europe/Brussels`).

### AC-6: Structured Logging & Opt-In Bounded Retries via Quartz Listener
- **Given** a recurring job executes under Quartz.NET
- **When** the job starts, completes, or throws an unhandled exception
- **Then** a shared Quartz `IJobListener` logs structured start, completion (with elapsed duration), and failure events via `ILogger`
- **And** if the job throws an unhandled exception (excluding `OperationCanceledException` during host shutdown) and the job declares `MaxRetries > 0` and the current retry attempt `< MaxRetries`:
  - The listener schedules a one-off delayed retry trigger (using the job's configured backoff delay, passing the incremented retry count in `JobDataMap`) without disturbing the recurring cron trigger.
- **And** if `MaxRetries == 0` or the retry limit is exhausted, the listener logs a final error and does not schedule further retries until the next cron fire time or manual admin trigger.

### AC-7: Admin-Only Built-In Blazor `Quartz.Dashboard` & Auth Bridge
- **Given** `Quartz.Dashboard` is hosted on `Guts.Api` at `/quartz`
- **When** securing the dashboard for Administrators only
- **Then** `Quartz.Dashboard` is protected by a dedicated Admin-only cookie authorization policy (e.g., `QuartzDashboardAdminPolicy` requiring the dedicated Quartz dashboard cookie scheme + `Role.Constants.Admin`), so unauthenticated, Student, and Lector requests to `/quartz` or `/_blazor` are denied
- **And** `ScheduledJobsController` exposes a secure two-step JWT-to-cookie bridge so Admins logged into the Angular SPA (even across separate web/API origins) can open the dashboard seamlessly:
  1. `POST /api/scheduled-jobs/dashboard-ticket` (`[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]`, blocked during impersonation by `ImpersonationReadOnlyFilter`): generates a cryptographically random, single-use, short-lived (e.g., 60-second TTL) ticket stored in `IMemoryCache` bound to the Admin's identity/claims and returns `{ url: "<apiBaseUrl>/api/scheduled-jobs/dashboard-login?ticket=<ticket>" }` (or `{ ticket }`).
  2. `GET /api/scheduled-jobs/dashboard-login?ticket=<ticket>` (`[AllowAnonymous]`): atomically consumes the single-use ticket (invalidating it immediately so it cannot be replayed), signs in the Admin under the dedicated Quartz dashboard `HttpOnly`, `Secure` (in HTTPS), `SameSite=Lax` cookie scheme (short sliding/absolute lifetime, e.g., 30 minutes), and redirects (`302 Found`) to `/quartz`.
- **And** in the Angular SPA (`SidebarMenuComponent`), when `userProfile.isAdmin() && !isImpersonating`, a **"Scheduled Jobs"** item appears under the **Admin** section:
  - Clicking it requests a one-time dashboard ticket via `ScheduledJobsService` and opens the returned dashboard login URL in a new browser tab (`target="_blank"`, `rel="noopener noreferrer"`), landing the Admin directly in the live Blazor `Quartz.Dashboard`.
  - If the ticket request fails, an error toast/notification is displayed to the Admin.

---

## 4. Domain Rules & Invariants

_Scheduling is an infrastructure/application-hosting concern rather than a core educational domain aggregate. Existing domain invariants (`IsoWeek`, `PeerAssessmentReminderPolicy`, and `MessageTracker`) remain strictly encapsulated in `Guts.Domain`._

- **DR-1 (Unique Job Names & Valid Cron Expressions)**: Every `IRecurringJob` must define a non-empty, unique `JobName` and a valid Quartz cron expression (`CronExpression.IsValidExpression(...)`); invalid configuration fails fast on startup.
- **DR-2 (Non-Overlapping Execution)**: A recurring job must never execute concurrently with another instance of itself (enforced via `[DisallowConcurrentExecution]` and Quartz clustered locks).
- **DR-3 (Local ISO-Week Consistency for Weekly Sprint)**: `SprintLeaderboardJob` must derive the completed ISO week from the job's local `Europe/Brussels` fire timestamp so Monday `00:01` CET/CEST always evaluates the week that just ended on Sunday.
- **DR-4 (Idempotent Streak Reminder Tracking)**: `StreakReminderService` uses `MessageTracker` with `EntityType = "StreakReminder:{year}-W{weekNumber:D2}"` (max 64 chars) and `EntityId = courseId`, enforcing at-most-once successful delivery and max 3 failed attempts per `(course, ISO week, student email)` via `MessageTracker.CanRetry` / `Retry` / `MarkSucceeded` / `MarkFailed`.

---

## 5. Authorization & Security Rules

- **Administrators**:
  - Permitted to call `POST /api/scheduled-jobs/dashboard-ticket` (`ApiConstants.AdminOnlyPolicy`) and access `/quartz` via the signed-in Admin dashboard cookie.
- **Impersonation**:
  - `POST /api/scheduled-jobs/dashboard-ticket` is blocked during impersonation by `ImpersonationReadOnlyFilter` (`403 Forbidden`), and the "Scheduled Jobs" sidebar link is hidden while impersonating.
- **Lectors & Students**:
  - Denied `POST /api/scheduled-jobs/dashboard-ticket` (`403 Forbidden`) and denied direct access to `/quartz` (`401`/`403`).
- **Unauthenticated**:
  - Denied `POST /api/scheduled-jobs/dashboard-ticket` (`401 Unauthorized`) and denied `/quartz`.
  - Invalid, expired, or already-used tickets on `GET /api/scheduled-jobs/dashboard-login` are rejected with `401 Unauthorized` (never issuing a cookie).

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Non-admin (Student or Lector) calls `POST /api/scheduled-jobs/dashboard-ticket` | Rejected by `AdminOnlyPolicy` | `403 Forbidden` |
| Admin impersonating another user calls `POST /api/scheduled-jobs/dashboard-ticket` | Rejected by `ImpersonationReadOnlyFilter` | `403 Forbidden` |
| Unauthenticated call to `POST /api/scheduled-jobs/dashboard-ticket` | Rejected by default JWT auth filter | `401 Unauthorized` |
| Missing, expired, or replayed `ticket` on `GET /api/scheduled-jobs/dashboard-login` | Ticket rejected; no cookie issued | `401 Unauthorized` |
| Unauthenticated browser request directly to `/quartz` | Denied by `QuartzDashboardAdminPolicy` | `401 Unauthorized` / `403 Forbidden` |
| Invalid cron expression or timezone ID in `appsettings.json` | Fail fast with descriptive startup exception | N/A (Startup) |
| Host shuts down while a job is running | Graceful cancellation via `CancellationToken` + `WaitForJobsToComplete = true`; Quartz marks state cleanly | N/A (Background) |
| Server down during Monday `00:01` or Thursday `12:00` | Quartz `FireAndProceed` misfire policy triggers the missed job once on recovery | N/A (Background) |
| `PeerAssessmentReminderJob` misses ticks during downtime | Quartz `DoNothing` misfire policy skips missed ticks and waits for next 2-hour schedule | N/A (Background) |
| `StreakReminderJob` fails midway and retries | Already-sent students have `MessageTracker.Status == Succeeded` and are skipped; failed students with `RetryCount < 3` are retried | N/A (Background) |
| Job throws exception with `MaxRetries > 0` | Listener schedules bounded delayed retry trigger up to `MaxRetries` | N/A (Background) |

---

## 7. Non-Functional & Quality Gate Requirements

- **Test Coverage**: Unit tests must cover as many lines and branches of all new and modified code as possible across `Guts.Application.Tests` (`StreakReminderServiceTests` for `MessageTracker` idempotency & retry), `Guts.Api.Tests` (all `IRecurringJob` classes, startup sync service, retry listener, `ScheduledJobsController`, dashboard ticket store), `Guts.Architecture.Tests` (verifying layer boundaries and controller security), and Angular unit tests (`*.spec.ts` for `ScheduledJobsService` and `SidebarMenuComponent`), satisfying the SonarCloud new-code coverage Quality Gate (`>= 80%`, targeting maximum achievable coverage).
- **Playwright E2E Coverage**: New/updated Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/` verifying that the "Scheduled Jobs" Admin sidebar item is visible only to non-impersonating Admins, calls `POST /api/scheduled-jobs/dashboard-ticket`, opens the dashboard URL, and handles error states gracefully.
- **Static Analysis & Security**: SonarCloud / SonarAnalyzer checks must pass with `0` open issues, `0` unreviewed security hotspots, and `0` ESLint errors.

---

## 8. Out of Scope

- Migrating `DeletionBackgroundService` / `InMemoryDeletionJobQueue` (event-driven queue worker remains separate).
- Custom Angular UI re-implementing Quartz job management (replaced by the official built-in Blazor `Quartz.Dashboard`).
- Implementing the upcoming new weekly task itself (this spec establishes the platform so adding it afterward is a single `IRecurringJob` class).

---

## 9. Open Questions

- [x] **Q1**: Should we use the official built-in Blazor `Quartz.Dashboard` instead of building a custom Angular dashboard? — *Decision: Yes — host `Quartz.Dashboard` at `/quartz`, secured strictly for Admins via a single-use short-lived ticket ➔ `HttpOnly` cookie bridge, linked from the Angular Admin sidebar.*
- [x] **Q2**: Should making `StreakReminderService` idempotent via `MessageTracker` be included in scope? — *Decision: Yes — reuse `MessageTracker` so `StreakReminderJob` supports safe automatic retries (`MaxRetries = 3`) and manual re-triggers without duplicate emails.*
