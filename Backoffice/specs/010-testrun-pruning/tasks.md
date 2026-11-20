# TDD Implementation Plan: Automated Pruning of Redundant Test Runs

- **Feature ID**: `010-testrun-pruning`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-1, AC-2, DR-1, DR-2, DR-3, DR-4]` Write failing zero-mock unit tests in `Backoffice/Guts.Domain.Tests/TestRunAggregate/TestRunPruningPolicyTests.cs` covering:
  - Null/empty or single-run input returns empty list.
  - Consecutive identical full runs for the same user keep only the oldest run and mark newer ones redundant (including trailing runs).
  - Unordered input is sorted strictly by `(CreateDateTime ASC, TestRunId ASC)` so the oldest run is always preserved (and ties on timestamp break by `TestRunId`).
  - Partial single-test runs: unchanged partial run is pruned; partial run that flips red ➔ green or green ➔ red or introduces a new `TestId` is kept and updates cumulative state.
  - State oscillation (Fail ➔ Pass ➔ Fail ➔ Fail): first 3 kept, 4th pruned.
  - Empty first run is kept; subsequent empty runs are pruned.
  - Duplicate `TestId` rows within the same run fold deterministically.
  - Independent users in a Chapter assignment are evaluated in separate partitions.
  - Project assignment with `userIdToTeamId`: teammates are evaluated in a single combined team partition (Alice Pass ➔ Bob Fail ➔ Alice Pass keeps all 3; Alice Pass ➔ Bob Pass prunes Bob's run), while unassigned users (e.g., lectors) fall back to their own per-user partition.
  - Re-running policy after removing returned redundant IDs returns empty list (idempotency).
- [x] **[T1.2]** `[AC-1, AC-2, DR-1, DR-2, DR-3, DR-4]` Implement `TestResultSnapshot`, `TestRunSnapshot`, and `TestRunPruningPolicy` in `Backoffice/Guts.Domain/TestRunAggregate/TestRunPruningPolicy.cs`.
- [x] **[T1.3]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Application Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-4, AC-6, AC-7, DR-5]` Expand `ITestRunRepository` in `Backoffice/Guts.Application/Repositories/ITestRunRepository.cs` with `PruningAssignmentTarget`, `TestRunBatchDeletionResult`, and the four pruning query/delete methods; create `TestRunPruningResult` in `Backoffice/Guts.Application/Dtos/TestRunPruningResult.cs` and `ITestRunPruningService` in `Backoffice/Guts.Application/Services/ITestRunPruningService.cs`.
- [x] **[T2.2]** `[AC-4, AC-6, AC-7, DR-5]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/TestRunPruningServiceTests.cs` covering:
  - `BatchSize <= 0` throws `ContractException`.
  - No active period (`DataNotFoundException` or `null`) or `activePeriod.IsDeleting == true` returns `(0, 0, 0)` without querying assignments.
  - Scans active-period Chapter and Project assignments, caches `GetProjectTeamMembershipMapAsync` once per `projectId` across multiple components, delegates redundancy detection to `TestRunPruningPolicy`, chunks deletions by `batchSize`, and returns accumulated `(AssignmentsScanned, TestRunsDeleted, TestResultsDeleted)`.
  - Skips deletion when no redundant runs exist for an assignment.
  - Honors `CancellationToken` cleanly.
- [x] **[T2.3]** `[AC-4, AC-6, AC-7, DR-5]` Implement `TestRunPruningService` in `Backoffice/Guts.Application/Services/TestRunPruningService.cs`.
- [x] **[T2.4]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-3, AC-4, AC-6]` Write failing repository integration tests in `Backoffice/Guts.Infrastructure.Tests/Repositories/TestRunDbRepositoryTests.cs` covering:
  - `GetPruningAssignmentTargetsForPeriodAsync`: returns only assignments in the target period with `> 1` test run, excludes non-active periods, excludes `Course.IsDeleting == true` and `Period.IsDeleting == true`, and accurately distinguishes `IsProject` (`Chapter` vs `Project`).
  - `GetTestRunSnapshotsForAssignmentAsync`: returns accurate lightweight `(TestRunId, UserId, CreateDateTime, Results)` snapshots and returns early without error when `<= 1` run exists.
  - `GetProjectTeamMembershipMapAsync`: returns `UserId -> ProjectTeamId` map scoped to the specified project.
  - `DeleteTestRunsBatchAsync`: deletes `TestResults` and `TestRuns` for specified IDs while leaving kept runs, **all `TestCodeHash` rows**, and **all `SolutionFile` rows** completely intact; handles empty input cleanly.
- [x] **[T3.2]** `[AC-3, AC-4, AC-6]` Implement the four methods in `Backoffice/Guts.Infrastructure/Repositories/TestRunDbRepository.cs`.
- [x] **[T3.3]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-5, AC-7]` Write failing unit tests in `Backoffice/Guts.Api.Tests/ScheduledJobs/TestRunPruningJobTests.cs` covering:
  - `RecurringJobDescriptor.FromType(typeof(TestRunPruningJob))` metadata (`JobName = "TestRunPruning"`, `DefaultCronExpression = "0 0 3 ? * SUN"`, `MisfirePolicy.FireAndProceed`, `MaxRetries = 3`, `RetryDelay = 15m`).
  - `Execute` uses default batch size (`500`) when unconfigured and honors `TestRunPruning:BatchSize` override from `IConfiguration`, delegating to `ITestRunPruningService.PruneRedundantTestRunsAsync` with `cancellationToken`.
- [x] **[T4.2]** `[AC-5, AC-7]` Implement `TestRunPruningJob` in `Backoffice/Guts.Api/ScheduledJobs/Jobs/TestRunPruningJob.cs` and add `TestRunPruning` configuration to `Backoffice/Guts.Api/appsettings.json`.
- [x] **[T4.3]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj` and verify all tests pass.

---

## Phase 5: Final Verification & Quality Gate

- [x] **[T5.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [x] **[T5.2]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and inspect per-file new-code coverage; add missing tests for any uncovered lines/branches until Quality Gate is `OK` (`>= 80%`, aiming as close to `100%` as possible, `0` open issues, and `0` unreviewed hotspots).
- [x] **[T5.3]** Update `spec.md` and `design.md` status to `Implemented`.
