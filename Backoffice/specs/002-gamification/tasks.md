# TDD Implementation Plan: Summarize Passed Test Events in Recent XP Gains (US-4 / AC-5)

- **Feature ID**: `002-gamification`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. Write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain`)

- *No Domain Layer changes required (`StudentCourseProfile.Events` continues to store individual `GamificationEvent` entities).*

---

## Phase 2: Business Layer (`Guts.Business.Tests` ➔ `Guts.Business`)

- [x] **[T2.1]** `[US-4, AC-5]` Write failing unit tests in `Backoffice/Guts.Business.Tests/Services/GamificationServiceTests.cs`:
  - `GetStudentCourseProfileAsync_WithMultiplePassedTests_ShouldSummarizePassedTestsIntoSingleEvent`: Verify that multiple `PassedTest` events after `LastLeaderboardVisitDate` are aggregated into a single `RecentXpEventDto` with `Description = "Made {count} tests green"`, `XpEarned = count * 10`, and `CreatedAt = latestPassedTestTimestamp`, while bonus events (e.g., `AssignmentCompleted`) remain separate entries ordered by `CreatedAt` descending.
  - `GetStudentCourseProfileAsync_WithSinglePassedTest_ShouldUseSingularDescription`: Verify that 1 `PassedTest` event produces `Description = "Made 1 test green"` and `XpEarned = 10`.
- [x] **[T2.2]** `[US-4, AC-5]` Update `GamificationService.GetStudentCourseProfileAsync` in `Backoffice/Guts.Business/Services/GamificationService.cs` to aggregate `GamificationEventType.PassedTest` events in `RecentGains.Events` and keep other bonus events separate.
- [x] **[T2.3]** Run `dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj` and verify all tests pass.

---

## Phase 3: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T3.1]** `[US-4, AC-5]` Update unit test in `Backoffice/Guts.Web/ClientApp/src/app/components/course-gamification/course-gamification-hero.component.spec.ts` to reflect the summarized description (`"Made 10 tests green"`, `+100 XP`).
- [x] **[T3.2]** Run `npm test -- --watch=false` in `Backoffice/Guts.Web/ClientApp` and verify all tests pass.

---

## Phase 4: Final Verification & Quality Gate

- [x] **[T4.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
