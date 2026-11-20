# Specification: Automated Pruning of Redundant Test Runs

- **Feature ID**: `010-testrun-pruning`
- **Status**: `Implemented`
- **Created**: `2026-11-20`

---

## 1. Context & Problem Statement

Students and teachers frequently execute monitored unit tests repeatedly while working on exercises and project components—often producing long streaks of consecutive `TestRun` records (and their child `TestResult` rows) where the pass/fail outcome of every test remains unchanged. Over multiple academic periods, these redundant `TestRun` and `TestResult` rows accumulate into the largest tables in the GUTS SQL Server database without adding value to grading, progress tracking, or point-in-time (`dateUtc`) queries.

This feature introduces a weekly **Quartz.NET recurring job** (`TestRunPruningJob`, built on the `009-scheduled-jobs` platform) backed by a pure domain policy (`TestRunPruningPolicy`) that scans all assignments in the **current active academic period** and removes newer redundant `TestRun`s (and their child `TestResult`s) that produced **no change in cumulative pass/fail test state**, keeping the **oldest** run of each unchanged streak.

Crucially:
- All registered teacher **`TestCodeHash`** records (stored on `Assignment`) remain completely untouched.
- All **`SolutionFile`** records remain untouched (they are already content-deduplicated via `IsNewVersionOf`, and preserving them guarantees students' post-green refactored code and historical code snapshots are never lost).
- Project Components are evaluated **per team** (`ProjectTeam`) so interleaved runs by different teammates never corrupt the team's latest test status.

---

## 2. User Stories & Actors

- **Actors**: `Administrator`, `Background Scheduler (Quartz.NET: TestRunPruningJob)`

- **US-1 (Database Space Reclamation)**: As an **Administrator**, I want redundant `TestRun` and `TestResult` records in the active academic period (for both students and lectors) to be pruned automatically every week so that database storage growth stays bounded and queries remain fast without altering archived past periods.
- **US-2 (Accurate Progress & Point-in-Time History)**: As a **Lector**, I want pruning to preserve every state-changing `TestRun` (keeping the oldest run of each unchanged streak, including partial single-test runs and team-interleaved project runs) as well as all `SolutionFiles` and `TestCodeHash` entries so that student/team scores, exam `dateUtc` snapshots, refactored code, and anti-tampering validation remain 100% accurate.
- **US-3 (Safe, Observable & Recoverable Execution)**: As an **Administrator**, I want the pruning job to run in safe SQL batches without locking the database, log structured summary metrics, recover from downtime via Quartz misfire/retry policies, and be manually triggerable or pausable from the Admin Quartz Dashboard.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Cumulative Pass/Fail Redundancy Detection for Chapters (Individual Work)
- **Given** an `Assignment` belonging to a `Chapter` and all `TestRun`s for a specific `UserId`, ordered chronologically by `(CreateDateTime ASC, Id ASC)`
- **When** `TestRunPruningPolicy` evaluates the ordered sequence of runs while maintaining the cumulative state (`TestId -> Passed`):
  - If a run introduces at least one `TestId` not yet present in the cumulative state, **OR** changes `Passed` (`true <-> false`) for at least one `TestId` compared to its current cumulative value ➔ the run is **kept** and the cumulative state is updated with all `TestResult`s in that run.
  - If the run is the very first run for that `(Assignment, User)` and has `0` `TestResult`s ➔ it is **kept**.
  - Otherwise (every `TestResult` in the run matches the existing cumulative `Passed` value for its `TestId`, including partial single-test runs, full identical re-runs, trailing unchanged runs, runs whose only difference is `TestResult.Message`, or subsequent empty runs) ➔ the run is marked **prunable** (keeping the oldest run of the unchanged streak).
- **Then** only the prunable `TestRun`s and their child `TestResult`s are deleted.

### AC-2: Team-Aware Redundancy Detection for Project Components (Team Work)
- **Given** an `Assignment` belonging to a `Project`
- **When** `TestRunPruningPolicy` partitions runs before evaluating chronological cumulative state:
  - Runs submitted by users who belong to a `ProjectTeam` in that `Project` are grouped by `(AssignmentId, ProjectTeamId)` and evaluated chronologically across **all teammates' runs combined**.
  - Runs submitted by users who are **not** a member of any `ProjectTeam` in that `Project` (e.g., a Lector running tests or an unassigned student) are grouped individually by `(AssignmentId, UserId)`.
- **Then** interleaved team runs (e.g., Alice passes T1 at `10:00` ➔ Bob fails T1 at `10:05` ➔ Alice passes T1 at `10:10`) are all **kept** because each run changes the team's cumulative state, preventing a regression of team test status; whereas consecutive team runs with no change in team cumulative state prune the newer run.

### AC-3: Preservation of `TestCodeHash`, `SolutionFiles`, and Gamification Data
- **Given** assignments with registered teacher `TestCodeHash` entries, `SolutionFile` versions, and `GamificationEvent` / `StudentBadge` / `StudentCourseProfile` records
- **When** the pruning job executes across all users (students and lectors)
- **Then**:
  - Zero `TestCodeHash` rows are deleted or modified—all teacher test-code hashes remain valid.
  - Zero `SolutionFile` rows are deleted or modified—all student/team code submissions (including refactored code submitted during a pruned run) are preserved.
  - Zero gamification records (`GamificationEvent`, `StudentBadge`, `StudentCourseProfile`) are modified.

### AC-4: Active-Period Scope, Deletion Safety & Chunked Batch Execution
- **Given** the current active academic `Period` resolved via `IPeriodRepository.GetPeriodAsync()` (with no age cutoff for runs within that period)
- **When** `ITestRunPruningService.PruneRedundantTestRunsAsync(int batchSize, CancellationToken cancellationToken)` runs
- **Then**:
  - Only `Assignment` records belonging to topics in the **active period** are scanned; assignments in past or future periods are never modified.
  - If no active period exists (e.g., `DataNotFoundException` or `null`) or the active period has `IsDeleting == true`, the job logs an informational message and exits cleanly with `0` deletions.
  - Assignments whose `Topic.Course.IsDeleting == true` are skipped to avoid contention with `DeletionBackgroundService` (`006-chunked-cascade-deletion`).
  - Assignments are processed one at a time using lightweight read-only projections (`TestRunId`, `UserId`, `CreateDateTime`, `TestId`, `Passed` — without loading `TestResult.Message` or full entity graphs).
  - Identified redundant `TestRunId`s are deleted in configurable chunks (default `BatchSize = 500` runs per batch), deleting child `TestResults` before parent `TestRuns` inside each batch to satisfy the `DeleteBehavior.Restrict` FK constraint.
  - `CancellationToken` is honored cleanly between assignments and batches.

### AC-5: Quartz.NET Weekly Recurring Job (`TestRunPruningJob`)
- **Given** the `009-scheduled-jobs` Quartz.NET infrastructure
- **When** `TestRunPruningJob` is implemented as an `IRecurringJob` with `[DisallowConcurrentExecution]`
- **Then**:
  - `JobName = "TestRunPruning"`
  - `DefaultCronExpression = "0 0 3 ? * SUN"` (weekly Sunday `03:00 Europe/Brussels`)
  - `MisfirePolicy = MisfirePolicy.FireAndProceed`
  - `MaxRetries = 3`, `RetryDelay = TimeSpan.FromMinutes(15)`
  - Auto-discovered by `AddScheduledJobs(...)` and configurable in `appsettings.json` under `ScheduledJobs:Jobs:TestRunPruning` (with optional `TestRunPruning:BatchSize`, default `500`).
  - Visible, pausable, resumable, and manually triggerable by Administrators in the built-in Blazor Quartz Dashboard (`/quartz`).

### AC-6: Idempotency & Crash Resilience
- **Given** a partially pruned or fully pruned database (e.g., after a host restart mid-job or a manual re-trigger)
- **When** `TestRunPruningJob` runs again
- **Then** the result is completely idempotent: remaining unpruned redundant runs are cleanly removed, already-pruned assignments produce `0` deletions, and cumulative test outcomes for every `(Assignment, User/Team)` at any point in time (`dateUtc`) remain identical before and after pruning.

### AC-7: Structured Summary Logging
- **Given** a completed (or cancelled) execution of `TestRunPruningJob` / `TestRunPruningService`
- **When** pruning finishes
- **Then** structured log entries record `AssignmentsScanned`, `TestRunsDeleted`, `TestResultsDeleted`, and elapsed duration via `ILogger`.

---

## 4. Domain Rules & Invariants

All redundancy-evaluation logic must reside in `Guts.Domain` (zero business logic in `Guts.Application` or `Guts.Infrastructure`):

- **DR-1 (Chronological Ordering)**: Runs within a partition are evaluated strictly in `(CreateDateTime ASC, TestRunId ASC)` order so the oldest run of any unchanged streak is always preserved.
- **DR-2 (Cumulative Pass/Fail Equivalence)**: A run causes a state change if and only if it contains at least one `(TestId, Passed)` entry where either `TestId` has not been seen yet in the partition's cumulative state, or `Passed` differs from the cumulative state's current `Passed` value for that `TestId`. `TestResult.Message` differences alone do **not** constitute a state change.
- **DR-3 (Empty First Run Preservation)**: If the chronologically first run in a partition has `0` test results, it is kept; any subsequent run with `0` test results causes no state change and is pruned.
- **DR-4 (Duplicate `TestId` Within Same Run)**: If a single `TestRun` contains multiple `TestResult` rows for the same `TestId`, evaluation folds them deterministically (any entry differing from the prior cumulative state marks the run as kept, and the final state for that `TestId` updates the cumulative map).
- **DR-5 (Batch Size Precondition)**: `BatchSize` must be strictly positive (`Contracts.Require(batchSize > 0, ...)`).

---

## 5. Authorization & Security Rules

- **No new HTTP API endpoints or Angular routes** are added.
- Manual execution, pausing, resuming, and live monitoring are exclusively available to **Administrators** via the existing Admin-only Blazor Quartz Dashboard (`/quartz`, `QuartzDashboardAdminPolicy`).
- **Lectors, Students, and unauthenticated users** have no access to trigger or control the pruning job.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| `BatchSize <= 0` passed to pruning service | Throws `ContractException` (`Contracts.Require`) | N/A (Background) |
| Assignment has `0` test runs or `1` test run | Skipped cleanly; `0` deletions | N/A (Background) |
| Partial single-test run matches previous pass/fail status | Pruned (no change in cumulative state) | N/A (Background) |
| Partial single-test run flips a test from red to green (or green to red) | Kept; cumulative state updated | N/A (Background) |
| Project team members alternate Pass ➔ Fail ➔ Pass on the same test | All 3 runs kept because each changes team cumulative state | N/A (Background) |
| Student submits a new run while pruning runs concurrently for an assignment | Only IDs evaluated in the snapshot are deleted; newer runs created after the query are never touched | N/A (Background) |
| No active academic period found or active period has `IsDeleting == true` | Logged and skipped cleanly with `0` deletions | N/A (Background) |
| Course has `IsDeleting == true` | Assignment skipped to avoid colliding with `DeletionBackgroundService` | N/A (Background) |
| Host shuts down mid-pruning | `CancellationToken` stops cleanly between batches; already-deleted batches are committed and valid; next run resumes idempotently | N/A (Background) |
| Transient SQL error during a batch | Exception bubbles to `RecurringJobListener`, which logs and schedules a bounded retry (up to 3 attempts) | N/A (Background) |

---

## 7. Non-Functional & Quality Gate Requirements

- **Test Coverage**: Unit and integration tests must cover as many lines and branches of all new and modified code as possible across `Guts.Domain.Tests` (pure zero-mock tests of `TestRunPruningPolicy` covering every edge case), `Guts.Application.Tests` (`TestRunPruningServiceTests`), `Guts.Infrastructure.Tests` (`TestRunPruningDbRepositoryTests` verifying batch deletion, `DeleteBehavior.Restrict` ordering, `IsDeleting` filtering, and preservation of `TestCodeHash` + `SolutionFiles`), and `Guts.Api.Tests` (`TestRunPruningJobTests`), satisfying the SonarCloud new-code coverage Quality Gate (`>= 80%`, targeting ~100%).
- **Static Analysis & Security**: SonarCloud / SonarAnalyzer checks must pass with `0` open issues and `0` unreviewed security hotspots.

---

## 8. Out of Scope

- Pruning `TestRun`s in past or future (non-active) academic periods.
- Pruning or compacting `SolutionFiles` (explicitly kept intact).
- Pruning or modifying `TestCodeHash` entries (explicitly kept intact).
- Pruning `GamificationEvent` or `StudentCourseProfile` records.
- Adding a dedicated Angular UI or dry-run API endpoint (Quartz Dashboard already provides Admin visibility and manual triggering).

---

## 9. Open Questions

- [x] **Q1**: How should "no change" be evaluated for partial runs and project teams? — *Decision: Cumulative pass/fail state per `(Assignment, User)` for Chapters and per `(Assignment, ProjectTeam)` for Projects (falling back to `(Assignment, User)` for unassigned users), ignoring `TestResult.Message`.*
- [x] **Q2**: Should `SolutionFiles` be deleted along with pruned `TestRun`s? — *Decision: No — leave `SolutionFiles` untouched so refactored code and code snapshots are preserved.*
- [x] **Q3**: Which periods and run ages are eligible? — *Decision: Active academic period only (past/future periods untouched), with no age cutoff.*
- [x] **Q4**: Should the trailing latest run be preserved if unchanged? — *Decision: No — strictly keep only state-changing runs (oldest of each unchanged streak, plus first run if empty).*
- [x] **Q5**: Schedule, misfire, and retry policy? — *Decision: Weekly Sunday `03:00 Europe/Brussels` (`0 0 3 ? * SUN`), `FireAndProceed`, `MaxRetries = 3`, `BatchSize = 500`.*
- [x] **Q6**: Should teacher (`Lector`) `TestRun`s be pruned, and what about teacher `TestCodeHash` entries? — *Decision: Prune redundant `TestRun`s for all users (students & lectors), keeping all `TestCodeHash` entries completely intact; report results via structured logging.*
