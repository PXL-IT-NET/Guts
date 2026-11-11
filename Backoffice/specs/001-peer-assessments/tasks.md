# TDD Implementation Plan: Peer Assessment Deadline Reminders & Message Tracking

- **Feature ID**: `001-peer-assessments`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[DR-12, AC-11]` Create `MessageTrackerBuilder` in `Guts.Domain.Tests/Builders/` with fluent defaults for test setup.
- [x] **[T1.2]** `[DR-12, AC-11]` Write failing unit tests in `Guts.Domain.Tests/MessageTrackerAggregate/MessageTrackerTests.cs` and `MessageTrackerFactoryTests.cs` covering:
  - `IMessageTrackerFactory.CreateNew` initializes with `Guid`, `Status = MessageStatus.Sending`, `CreatedAtUtc = nowUtc`, `SentAtUtc = nowUtc`, `RetryCount = 0`, and `FailureReason = null`.
  - Preconditions / invariant checks via `Contracts.Require`: empty `EntityType`, `EntityId <= 0`, empty `Email`, and non-UTC `DateTime` throw `ContractException`.
  - `MarkSucceeded()` requires `Status == MessageStatus.Sending` (throws `ContractException` otherwise), transitions to `MessageStatus.Succeeded`, and ensures `FailureReason = null`.
  - `MarkFailed(string reason)` requires `Status == MessageStatus.Sending` (throws `ContractException` otherwise), transitions to `MessageStatus.Failed`, increments `RetryCount` by 1, and records `FailureReason = reason`.
  - `Retry(DateTime sentAtUtc)` requires `CanRetry` (`Status == MessageStatus.Failed && RetryCount < 3`) and `sentAtUtc.Kind == DateTimeKind.Utc` (throws `ContractException` otherwise), transitions back to `MessageStatus.Sending`, updates `SentAtUtc = sentAtUtc`, and clears `FailureReason = null`.
  - `CanRetry` property returns `true` strictly when `Status == MessageStatus.Failed && RetryCount < 3`, and `false` otherwise.
- [x] **[T1.3]** `[DR-13, AC-10]` Write failing unit tests in `Guts.Domain.Tests/TopicAggregate/ProjectAggregate/PeerAssessmentReminderPolicyTests.cs` covering:
  - `IsAssessmentEligibleForReminders` returns `true` when assessment is open (`OpenOnUtc <= nowUtc < DeadlineUtc`), total duration $\ge 48$ hours (`(DeadlineUtc - OpenOnUtc) >= 48 hours`), and remaining time $\le 24$ hours (`(DeadlineUtc - nowUtc) <= 24 hours`).
  - Returns `false` when assessment is null, not yet open, deadline passed, total duration $< 48$ hours, or remaining time $> 24$ hours.
- [x] **[T1.4]** `[DR-12, DR-13, AC-10, AC-11]` Implement:
  - `MessageStatus.cs`, `IMessageTracker.cs`, `MessageTracker.cs`, `IMessageTrackerFactory.cs`, and `MessageTrackerFactory.cs` in `Guts.Domain/MessageTrackerAggregate/`.
  - `PeerAssessmentReminderPolicy.cs` in `Guts.Domain/TopicAggregate/ProjectAggregate/`.
- [x] **[T1.5]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all domain tests pass.

---

## Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-12]` Define `IMailThrottler` and implement `MailThrottler` in `Guts.Application/Communication/`. Write failing unit tests in `Guts.Application.Tests/Communication/MailThrottlerTests.cs` verifying delay execution and cancellation handling.
- [x] **[T2.2]** `[AC-12]` Refactor `StreakReminderService` to use injected `IMailThrottler` instead of its private `ThrottleDelay` property. Update `StreakReminderServiceTests.cs` to verify rate-limiting via mocked/configured `IMailThrottler`.
- [x] **[T2.3]** `[AC-13]` Add `SendPeerAssessmentReminderEmailAsync` to `IMailSender` and implement in `MailSender.cs`. Write failing unit tests in `Guts.Application.Tests/Communication/MailSenderTests.cs` verifying email subject, greeting, deadline UTC format, and direct URL to the team evaluation form (`courses/{courseId}/projects/{projectCode}/assessments/{assessment.Id}/teams/{teamId}/evaluate`).
- [x] **[T2.4]** `[AC-10, AC-11]` Define repository contracts:
  - `IMessageTrackerRepository` in `Guts.Application/Repositories/`.
  - Extend `IProjectAssessmentRepository` with `Task<IReadOnlyList<IProjectAssessment>> GetOpenAssessmentsAsync(DateTime nowUtc)`.
  - Extend `IProjectRepository` with `Task<IProject> LoadWithCourseAsync(int projectId)`.
- [x] **[T2.5]** `[AC-10, AC-11, AC-12, AC-13, DR-12, DR-13]` Define `IPeerAssessmentReminderService` and write comprehensive failing unit tests in `Guts.Application.Tests/Services/Assessment/PeerAssessmentReminderServiceTests.cs` covering:
  - Filtering eligible assessments via `PeerAssessmentReminderPolicy`.
  - Loading project with course and teams for eligible assessments.
  - Auto-provisioning team assessments via `_projectTeamAssessmentService.GetOrCreateTeamAssessmentAsync`.
  - Querying pending peers via `teamAssessment.GetPeersThatNeedToEvaluateOthers()`.
  - New recipient: creates `MessageTracker` in `Sending` state, persists via `AddAsync`, sends email via `IMailSender`, marks `Succeeded`, persists update, and throttles.
  - Recipient already succeeded: skips sending email.
  - Recipient previously failed with `RetryCount < 3`: calls `tracker.Retry(executionTime)` (transitioning to `Sending` and clearing `FailureReason`), persists update, sends email, marks `Succeeded`, and persists.
  - Recipient previously failed with `RetryCount >= 3`: skips sending email.
  - Error during email transmission: calls `tracker.MarkFailed(ex.Message)`, persists update, logs error, and continues to remaining recipients.
- [x] **[T2.6]** `[AC-10, AC-11, AC-12, AC-13]` Implement `PeerAssessmentReminderService` in `Guts.Application/Services/Assessment/PeerAssessmentReminderService.cs`.
- [x] **[T2.7]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all application tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[DR-12, AC-11]` Create internal `MessageTrackerConfiguration` in `Guts.Infrastructure/EntityConfigurations/` mapping table `MessageTrackers` with composite PK `(EntityType, EntityId, Email)`. Register in `GutsContext`.
- [x] **[T3.2]** `[DR-12, AC-11]` Add EF Core migration `AddMessageTrackers` to `Guts.Infrastructure`.
- [x] **[T3.3]** `[AC-10, AC-11]` Implement repository methods:
  - `MessageTrackerDbRepository` in `Guts.Infrastructure/Repositories/MessageTrackerDbRepository.cs`.
  - `ProjectAssessmentDbRepository.GetOpenAssessmentsAsync(DateTime nowUtc)`.
  - `ProjectDbRepository.LoadWithCourseAsync(int projectId)`.
- [x] **[T3.4]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all infrastructure tests pass.

---

## Phase 4: API & Background Services Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-10]` Write failing unit tests in `Guts.Api.Tests/BackgroundServices/PeerAssessmentReminderBackgroundServiceTests.cs` covering periodic execution using configurable interval (default: 2 hours), scope creation, and error handling.
- [x] **[T4.2]** `[AC-10]` Implement `PeerAssessmentReminderBackgroundService` in `Guts.Api/BackgroundServices/`.
- [x] **[T4.3]** `[AC-10, AC-12]` Register `services.AddHostedService<PeerAssessmentReminderBackgroundService>();` in `Guts.Api/Startup.cs`. Register `IMailThrottler` in `Guts.Bootstrapper/StartUpExtensions.cs`. Add `PeerAssessmentReminders` and `Mail:ThrottleDelayInSeconds` in `Guts.Api/appsettings.json`.
- [x] **[T4.4]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all API tests pass.
- [x] **[T4.5]** Run `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj` and verify all ArchUnit architecture rules pass.

---

## Phase 5: Final Verification & Quality Gate

- [x] **[T5.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [x] **[T5.2]** Run frontend unit tests and production build check: `npm test -- --watch=false` and `npm run build` in `Backoffice/Guts.Web/ClientApp`.
- [x] **[T5.3]** Update `spec.md` and `design.md` status to `Implemented`.
