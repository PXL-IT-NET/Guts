# TDD Implementation Plan: Consolidated Scheduled Jobs (Quartz.NET) & Admin Dashboard

- **Feature ID**: `009-scheduled-jobs`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

_No new domain aggregates or value objects are required—existing `IsoWeek` (`Guts.Domain.ValueObjects`) and `MessageTracker` / `IMessageTrackerFactory` (`Guts.Domain.MessageTrackerAggregate`) already encapsulate all domain invariants (`DR-3`, `DR-4`)._

- [x] **[T1.1]** `[DR-3, DR-4]` Verify existing `Guts.Domain.Tests` (`IsoWeekTests`, `MessageTrackerTests`) pass as baseline: `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj`.

---

## Phase 2: Application Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-4, DR-4]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/StreakReminderServiceTests.cs` covering `MessageTracker` idempotency and retry handling:
  - Creates a new `MessageTracker` (`EntityType = "StreakReminder:{year}-W{weekNumber:D2}"`, `EntityId = course.Id`, `Email = student.Email`), persists it in `Sending` state, sends email, and marks `Succeeded`.
  - Skips students whose existing `MessageTracker` has `Status == Succeeded`, `Status == Sending`, or `Status == Failed` with `RetryCount >= 3`.
  - Retries students whose existing `MessageTracker` has `CanRetry == true` (`Status == Failed` and `RetryCount < 3`) via `tracker.Retry(executionTimeUtc)` before sending.
  - On `IMailSender` exception, calls `tracker.MarkFailed(ex.Message)`, persists it, logs the error, and continues processing remaining recipients.
  - Skips throttling when no email is actually sent (e.g., all recipients already tracked as `Succeeded`).
- [x] **[T2.2]** `[AC-4, DR-4]` Update `Backoffice/Guts.Application/Services/StreakReminderService.cs` to inject `IMessageTrackerRepository` and `IMessageTrackerFactory` (consolidating constructor overloads cleanly for DI auto-registration) and implement idempotent delivery tracking per `design.md`.
- [x] **[T2.3]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-1]` Add EF Core migration `AddQuartzSchema` in `Backoffice/Guts.Infrastructure/Migrations/` containing:
  - `Up`: Idempotent creation of schema `[quartz]` and the official Quartz.NET SQL Server `QRTZ_*` tables, primary keys, foreign keys, and indexes under `[quartz].[QRTZ_*]`.
  - `Down`: Dropping all `[quartz].[QRTZ_*]` tables in FK-safe order and dropping schema `[quartz]`.
- [x] **[T3.2]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-1, AC-7]` Add Quartz NuGet packages (`Quartz`, `Quartz.Extensions.Hosting`, `Quartz.Serialization.SystemTextJson`, `Quartz.Dashboard`) and `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>` to `Backoffice/Guts.Api/Guts.Api.csproj`.
- [x] **[T4.2]** `[AC-2, AC-3, DR-1, DR-2, DR-3]` Write failing unit tests in `Backoffice/Guts.Api.Tests/ScheduledJobs/`:
  - `SprintLeaderboardJobTests.cs`: verifies `Europe/Brussels` local fire-time ISO-week calculation (including Monday `00:01` CET/CEST when UTC is still Sunday evening) and delegation to `IGamificationService.AwardWeeklySprintBadgesAsync`.
  - `StreakReminderJobTests.cs`: verifies delegation to `IStreakReminderService.SendStreakRemindersAsync` with UTC fire time and cancellation token.
  - `PeerAssessmentReminderJobTests.cs`: verifies delegation to `IPeerAssessmentReminderService.SendAssessmentRemindersAsync` with UTC fire time and cancellation token.
- [x] **[T4.3]** `[AC-2, AC-3, DR-1, DR-2, DR-3]` Implement `MisfirePolicy`, `IRecurringJob`, `RecurringJobDescriptor`, `ScheduledJobsOptions`, and the three `[DisallowConcurrentExecution]` job classes (`SprintLeaderboardJob`, `StreakReminderJob`, `PeerAssessmentReminderJob`) in `Backoffice/Guts.Api/ScheduledJobs/`.
- [x] **[T4.4]** `[AC-6]` Write failing unit tests in `Backoffice/Guts.Api.Tests/ScheduledJobs/RecurringJobListenerTests.cs` covering:
  - Structured logging on start, completion, and failure.
  - Scheduling a delayed one-off retry trigger with incremented `RetryAttempt` when `MaxRetries > 0` and `retryAttempt < maxRetries`.
  - Stopping retries when `MaxRetries == 0`, when `retryAttempt >= maxRetries`, or when `CancellationToken.IsCancellationRequested`.
- [x] **[T4.5]** `[AC-6]` Implement `RecurringJobListener` in `Backoffice/Guts.Api/ScheduledJobs/RecurringJobListener.cs`.
- [x] **[T4.6]** `[AC-2, AC-5, DR-1]` Write failing unit tests in `Backoffice/Guts.Api.Tests/ScheduledJobs/ScheduledJobsSyncHostedServiceTests.cs` covering:
  - Creating missing durable jobs & cron triggers with defaults and `appsettings.json` overrides (`CronExpression`, `TimeZoneId`, `Enabled`).
  - Rescheduling existing triggers when cron expression, timezone, or misfire policy changes (and leaving unchanged triggers untouched).
  - Pausing triggers when `Enabled == false` and resuming when `Enabled == true`.
  - Failing fast on duplicate/empty `JobName`, invalid cron expression, or invalid timezone ID.
- [x] **[T4.7]** `[AC-2, AC-5, DR-1]` Implement `ScheduledJobsSyncHostedService` in `Backoffice/Guts.Api/ScheduledJobs/ScheduledJobsSyncHostedService.cs`.
- [x] **[T4.8]** `[AC-7]` Write failing unit tests in `Backoffice/Guts.Api.Tests/ScheduledJobs/QuartzDashboardTicketStoreTests.cs` and `Backoffice/Guts.Api.Tests/Controllers/ScheduledJobsControllerTests.cs` covering:
  - Generating cryptographically random tickets, single-use atomic consumption (second consume returns `null`), expiration, and Admin role claim preservation.
  - `POST /api/scheduled-jobs/dashboard-ticket` returning `200 OK` with `LoginUrl`.
  - `GET /api/scheduled-jobs/dashboard-login` returning `401 Unauthorized` for missing/invalid/replayed tickets and signing into `QuartzDashboardCookie` + redirecting to `/quartz` for valid tickets.
- [x] **[T4.9]** `[AC-7]` Implement `IQuartzDashboardTicketStore` / `MemoryQuartzDashboardTicketStore`, `QuartzDashboardTicketModel`, `ScheduledJobsController`, `ApiConstants` additions (`QuartzDashboardCookieScheme`, `QuartzDashboardAdminPolicy`), and `ScheduledJobsServiceCollectionExtensions.AddScheduledJobs(...)`.
- [x] **[T4.10]** `[AC-1, AC-3, AC-5, AC-7]` Wire `Startup.cs` (`AddScheduledJobs`, cookie auth scheme & `QuartzDashboardAdminPolicy`, `UseAntiforgery`, `MapQuartzDashboard` / `MapQuartzApi`), update `appsettings.json` (`ScheduledJobs` replacing `StreakReminders` & `PeerAssessmentReminders`), and remove the three legacy `BackgroundService` classes and their old tests (keeping `DeletionBackgroundService` & `InMemoryDeletionJobQueue` untouched).
- [x] **[T4.11]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T5.1]** `[AC-7]` Write failing Jasmine unit tests in `Backoffice/Guts.Web/ClientApp/src/app/services/scheduled-jobs.service.spec.ts` covering `createDashboardTicket()` success and HTTP error handling.
- [x] **[T5.2]** `[AC-7]` Implement `IQuartzDashboardTicketModel` and `ScheduledJobsService` in `Backoffice/Guts.Web/ClientApp/src/app/services/scheduled-jobs.service.ts`.
- [x] **[T5.3]** `[AC-7]` Update Jasmine unit tests in `Backoffice/Guts.Web/ClientApp/src/app/components/sidebar-menu/sidebar-menu.component.spec.ts` covering:
  - "Scheduled Jobs" item is visible only when `userProfile.isAdmin() && !isImpersonating` (hidden for Students, Lectors, and impersonating Admins).
  - Clicking "Scheduled Jobs" calls `scheduledJobsService.createDashboardTicket()` and opens the returned `loginUrl` in a new tab (`_blank`, `noopener,noreferrer`).
  - Failed ticket request shows an error notification and does not open a blank window.
- [x] **[T5.4]** `[AC-7]` Update `SidebarMenuComponent` (`sidebar-menu.component.ts` & `sidebar-menu.component.html`) to add the Admin-only "Scheduled Jobs" menu item and `openScheduledJobsDashboard()` handler.
- [x] **[T5.5]** `[AC-7]` Add/update Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/` verifying that Admins see the "Scheduled Jobs" link in the Admin sidebar (while Students/Lectors/impersonating Admins do not), clicking it requests `POST /api/scheduled-jobs/dashboard-ticket`, and error responses display feedback.
- [x] **[T5.6]** Run `npm run lint`, `npm test -- --watch=false`, `npm run build`, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp` and verify all pass.

---

## Phase 6: Final Verification & Quality Gate

- [x] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [x] **[T6.2]** Run frontend production build, lint, unit tests, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp`.
- [x] **[T6.3]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and inspect per-file new-code coverage; add missing tests for uncovered lines/branches across backend and frontend until Quality Gate is `OK` (`>= 80%`, aiming as close to `100%` as possible, `0` open issues, and `0` unreviewed hotspots).
- [x] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
