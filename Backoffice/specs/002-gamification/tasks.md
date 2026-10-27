# TDD Implementation Plan: Weekly Streak Loss & All-Tests-Passed Protection (US-7 / AC-6 / DR-3)

- **Feature ID**: `002-gamification`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Increment 1: Summarize Passed Test Events in Recent XP Gains (US-4 / AC-5) — *Completed*

- [x] **[T0.1]** `[US-4, AC-5]` Write failing unit tests in `Backoffice/Guts.Business.Tests/Services/GamificationServiceTests.cs` (`GetStudentCourseProfileAsync_WithMultiplePassedTests_ShouldSummarizePassedTestsIntoSingleEvent`, `GetStudentCourseProfileAsync_WithSinglePassedTest_ShouldUseSingularDescription`).
- [x] **[T0.2]** `[US-4, AC-5]` Update `GamificationService.GetStudentCourseProfileAsync` in `Backoffice/Guts.Business/Services/GamificationService.cs` to aggregate `GamificationEventType.PassedTest` events in `RecentGains.Events`.
- [x] **[T0.3]** `[US-4, AC-5]` Update unit test in `Backoffice/Guts.Web/ClientApp/src/app/components/course-gamification/course-gamification-hero.component.spec.ts` and verify all backend & frontend tests pass.

---

## Increment 2: Weekly Streak Loss & All-Tests-Passed Protection (US-7 / AC-6 / DR-3)

### Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[US-7, AC-6, DR-3]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/WeeklyStreakTests.cs` for `WeeklyStreak.NeedsEvaluation(DateTime timestamp)` and `WeeklyStreak.EvaluateStreak(DateTime timestamp, bool hasPassedAllAvailableTests)`:
  - `NeedsEvaluation`: Returns `false` when `CurrentStreak == 0`, `LastActiveYear == 0`, or `weeksDiff <= 1`; returns `true` only when `CurrentStreak > 0` and `weeksDiff > 1`.
  - `EvaluateStreak`:
    - When `!NeedsEvaluation(timestamp)`, returns the same `WeeklyStreak` instance unchanged.
    - When `NeedsEvaluation(timestamp)` is `true` and `hasPassedAllAvailableTests == false`, resets `CurrentStreak` to `0` while preserving `BestStreak`, `LastActiveYear`, and `LastActiveWeek` (and subsequent `NeedsEvaluation(timestamp)` returns `false`).
    - When `NeedsEvaluation(timestamp)` is `true` and `hasPassedAllAvailableTests == true`, keeps `CurrentStreak` and `BestStreak`, and advances `LastActiveYear` and `LastActiveWeek` to the immediately preceding ISO week so the streak can continue when new tests are added (and subsequent `NeedsEvaluation(timestamp)` in the same week returns `false`).
- [x] **[T1.2]** `[US-7, AC-6, DR-3]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/StudentCourseProfileTests.cs` for `StudentCourseProfile.NeedsWeeklyStreakEvaluation(DateTime timestamp)` and `StudentCourseProfile.EvaluateWeeklyStreak(DateTime timestamp, IReadOnlyCollection<int> allAvailableCourseTestIds)`:
  - When `NeedsWeeklyStreakEvaluation(timestamp)` is `false` (`CurrentStreak == 0` or `weeksDiff <= 1`), `EvaluateWeeklyStreak` short-circuits, leaves `WeeklyStreak` unchanged, and returns `false`.
  - When a week was skipped (`NeedsWeeklyStreakEvaluation == true`) and the student has not passed all available course tests (or when `allAvailableCourseTestIds` is empty), resets `WeeklyStreak.CurrentStreak` to `0` and returns `true`.
  - When a week was skipped (`NeedsWeeklyStreakEvaluation == true`) and the student has passed 100% of `allAvailableCourseTestIds` (`Count > 0`), keeps `WeeklyStreak.CurrentStreak` and advances `LastActiveWeek` to the previous ISO week, returning `true`.
- [x] **[T1.3]** `[US-7, AC-6, DR-3]` Implement `NeedsEvaluation` and `EvaluateStreak` in `Backoffice/Guts.Domain/GamificationAggregate/WeeklyStreak.cs`, and `NeedsWeeklyStreakEvaluation` and `EvaluateWeeklyStreak` in `Backoffice/Guts.Domain/GamificationAggregate/StudentCourseProfile.cs`.
- [x] **[T1.4]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

### Phase 2: Business Layer (`Guts.Business.Tests` ➔ `Guts.Business`)

- [x] **[T2.1]** `[US-7, AC-6]` Update repository contracts:
  - Add `Task<IReadOnlyList<int>> GetCourseTestIdsAsync(int courseId, int periodId)` to `IAssignmentRepository` in `Backoffice/Guts.Business/Repositories/IAssignmentRepository.cs`.
  - Add `Task<IReadOnlyList<StudentCourseProfile>> GetByCourseAndPeriodAsync(int courseId, int periodId)` and `Task UpdateRangeAsync(IEnumerable<StudentCourseProfile> entities)` to `IStudentCourseProfileRepository` in `Backoffice/Guts.Business/Repositories/IStudentCourseProfileRepository.cs`.
- [x] **[T2.2]** `[US-7, AC-6, DR-3]` Write failing unit tests in `Backoffice/Guts.Business.Tests/Services/GamificationServiceTests.cs`:
  - `GetStudentCourseProfileAsync_WhenStreakDoesNotNeedEvaluation_ShouldNotQueryCourseTestIds`: Verify `assignmentRepository.GetCourseTestIdsAsync` is **never** called when `NeedsWeeklyStreakEvaluation` is `false`.
  - `GetStudentCourseProfileAsync_WhenMissedLastWeekAndNotAllTestsPassed_ShouldResetCurrentStreakToZeroAndPersist`: Verify `CurrentWeeklyStreak` is `0`, `BestWeeklyStreak` is preserved, and `studentCourseProfileRepository.UpdateAsync` is called.
  - `GetStudentCourseProfileAsync_WhenMissedLastWeekButAllAvailableCourseTestsPassed_ShouldKeepStreakAndPersist`: Verify `CurrentWeeklyStreak` is preserved when the student's `PassedTest` events cover all test IDs returned by `assignmentRepository.GetCourseTestIdsAsync`.
  - `AwardWeeklySprintBadgesAsync_ShouldEvaluateWeeklyStreaksAndBatchSaveModifiedProfiles`: Verify weekly processing evaluates profiles with `NeedsWeeklyStreakEvaluation == true`, fetches `GetCourseTestIdsAsync` at most once per course when needed, resets expired streaks or preserves 100%-complete streaks, and batch-saves via `UpdateRangeAsync`.
- [x] **[T2.3]** `[US-7, AC-6, DR-3]` Update `GamificationService` in `Backoffice/Guts.Business/Services/GamificationService.cs` to implement the short-circuited lazy streak evaluation in `GetStudentCourseProfileAsync` and batch weekly streak evaluation in `AwardWeeklySprintBadgesAsync`.
- [x] **[T2.4]** Run `dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj` and verify all tests pass.

---

### Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[US-7, AC-6]` Write failing repository tests:
  - `GetCourseTestIdsAsync_ShouldReturnOnlyTestIdsForCourseAndPeriod` in `Backoffice/Guts.Infrastructure.Tests/Repositories/AssignmentDbRepositoryTests.cs`.
  - `GetByCourseAndPeriodAsync_And_UpdateRangeAsync_ShouldLoadAllProfilesAndPersistBatchUpdates` in `Backoffice/Guts.Infrastructure.Tests/Repositories/StudentCourseProfileDbRepositoryTests.cs`.
- [x] **[T3.2]** `[US-7, AC-6]` Implement `GetCourseTestIdsAsync` in `Backoffice/Guts.Infrastructure/Repositories/AssignmentDbRepository.cs`, and `GetByCourseAndPeriodAsync` + `UpdateRangeAsync` in `Backoffice/Guts.Infrastructure/Repositories/StudentCourseProfileDbRepository.cs`.
- [x] **[T3.3]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

### Phase 4: Final Verification & Quality Gate

- [x] **[T4.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [x] **[T4.2]** Verify code quality and clean build across all layers.
