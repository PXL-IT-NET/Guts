# TDD Implementation Plan: Course Gamification

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

## Increment 2: Weekly Streak Loss & All-Tests-Passed Protection (US-7 / AC-6 / DR-3) — *Completed*

- [x] **[T1.1]** `[US-7, AC-6, DR-3]` Write and pass unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/WeeklyStreakTests.cs` for `NeedsEvaluation` and `EvaluateStreak`.
- [x] **[T1.2]** `[US-7, AC-6, DR-3]` Write and pass unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/StudentCourseProfileTests.cs` for `NeedsWeeklyStreakEvaluation` and `EvaluateWeeklyStreak`.
- [x] **[T1.3]** `[US-7, AC-6, DR-3]` Implement `NeedsEvaluation` and `EvaluateStreak` in `WeeklyStreak.cs` and `StudentCourseProfile.cs`.
- [x] **[T2.1]** `[US-7, AC-6]` Update repository contracts (`IAssignmentRepository.GetCourseTestIdsAsync`, `IStudentCourseProfileRepository.GetByCourseAndPeriodAsync`, `UpdateRangeAsync`).
- [x] **[T2.2]** `[US-7, AC-6, DR-3]` Write and pass unit tests in `GamificationServiceTests.cs` and implement lazy + batch streak evaluation in `GamificationService.cs`.
- [x] **[T3.1]** `[US-7, AC-6]` Implement and test repository methods in `AssignmentDbRepository.cs` and `StudentCourseProfileDbRepository.cs`.

---

## Increment 3: Tiered Hall of Fame, Overtake/Tier-Up Bonuses, Teacher Exclusion & Student Perspective (US-3 / US-4 / US-5 / AC-2 / AC-4 / AC-5 / DR-7 / DR-8 / DR-9) — *Completed*

### Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T3.1]** `[AC-4, DR-7]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/HallOfFameLeagueTests.cs`:
  - Verify `HallOfFameLeague.GetTierForRank(int rank)` maps rank intervals accurately:
    - `1..15` ➔ `Obsidian` (`8`)
    - `16..35` ➔ `Diamond` (`7`)
    - `36..55` ➔ `Gold` (`6`)
    - `56..75` ➔ `Silver` (`5`)
    - `76..100` ➔ `Bronze` (`4`)
    - `101..150` ➔ `Iron` (`3`)
    - `151..200` ➔ `Wood` (`2`)
    - `>= 201` ➔ `Paper` (`1`)
  - Verify `GetTierForRank(<= 0)` throws `ContractException`.
  - Verify `HallOfFameLeague.GetVisibleTierWindow(HallOfFameTier currentTier)` returns a 4-tier progression window `[Previous, Current, Next1, Next2]` and clamps to 4 tiers at boundaries (`[Paper, Wood, Iron, Bronze]` at `Paper`; `[Silver, Gold, Diamond, Obsidian]` at `Diamond` and `Obsidian`).
- [x] **[T3.2]** `[AC-2, AC-4, DR-7, DR-8]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/StudentCourseProfileTests.cs`:
  - Verify `LastXpEarnedAt` returns the maximum `CreatedAt` among positive-XP events, or `DateTime.MaxValue` when no positive-XP events exist.
  - Verify `EvaluateHallOfFameProgression(previousRank, newRank, timestamp)`:
    - Awards `+4 XP` per newly surpassed rank (`GamificationEventType.HallOfFameOvertake`) and updates `BestHallOfFameRank`.
    - Awards `+5 XP` per newly reached `HallOfFameTier` (`GamificationEventType.HallOfFameTierUp`) and updates `BestHallOfFameTier`.
    - Does not re-award XP for ranks or tiers already achieved previously (anti-farming invariant `DR-8`).
    - Throws `ContractException` if `previousRank <= 0` or `newRank <= 0`.
- [x] **[T3.3]** `[AC-2, AC-4, DR-7, DR-8]` Implement `HallOfFameTier.cs`, `HallOfFameLeague.cs`, updated `GamificationEventType.cs`, and `StudentCourseProfile.EvaluateHallOfFameProgression` + `LastXpEarnedAt` in `Backoffice/Guts.Domain/GamificationAggregate/`.
- [x] **[T3.4]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

### Phase 2: Business Layer (`Guts.Business.Tests` ➔ `Guts.Business`)

- [x] **[T3.5]** `[AC-2, AC-4, DR-9]` Update `IStudentCourseProfileRepository` (`IsUserLectorAsync`), `IGamificationService` (`GetHallOfFameLeaderboardAsync` returning `HallOfFameLeaderboardDto`), and DTOs in `Backoffice/Guts.Business/Dtos/Gamification/GamificationDtos.cs`.
- [x] **[T3.6]** `[AC-2, AC-4, AC-5, DR-7, DR-8, DR-9]` Write failing unit tests in `Backoffice/Guts.Business.Tests/Services/GamificationServiceTests.cs`:
  - `ProcessTestRunAsync_WhenUserIsLector_ShouldSkipGamificationProcessing` (`[AC-2, DR-9]`).
  - `ProcessTestRunAsync_WhenStudentOvertakesAndRisesTier_ShouldAwardOvertakeAndTierUpBonusXp` (`[AC-2, DR-8]`).
  - `GetHallOfFameLeaderboardAsync_WithEqualTotalXp_ShouldRankStudentWhoReachedXpFirstHigher` (`[AC-4, DR-7]`).
  - `GetHallOfFameLeaderboardAsync_ForStudent_ShouldReturnOnlyCurrentTierEntriesVisibleTiersAndNextTarget` (`[AC-4, DR-7]`).
  - `GetHallOfFameLeaderboardAsync_ForLector_ShouldReturnAllLevelsOrSelectedStudentPerspective` (`[AC-4, US-5]`).
  - `GetStudentCourseProfileAsync_WithOvertakeAndTierUpEvents_ShouldIncludeInRecentGains` (`[AC-5]`).
- [x] **[T3.7]** `[AC-2, AC-4, AC-5, DR-7, DR-8, DR-9]` Implement teacher exclusion, Hall of Fame tie-breaking, tiered leaderboard slicing, 4-icon window, next target calculation, student perspective support, and overtake/tier-up evaluation in `Backoffice/Guts.Business/Services/GamificationService.cs`.
- [x] **[T3.8]** Run `dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj` and verify all tests pass.

---

### Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.9]** `[DR-8]` Persist and compute `BestHallOfFameRank` and `BestHallOfFameTier` via `GamificationEvents` (`HallOfFameOvertake` and `HallOfFameTierUp` `TargetId`).
- [x] **[T3.10]** `[AC-2, AC-4, DR-9]` Write failing repository tests in `Backoffice/Guts.Infrastructure.Tests/Repositories/StudentCourseProfileDbRepositoryTests.cs`:
  - Verify `IsUserLectorAsync` returns `true` for a user with the `Lector` role and `false` for a student.
  - Verify `GetForLeaderboardByCourseAndPeriodAsync` and `GetByCourseAndPeriodAsync` exclude users with the `Lector` role.
- [x] **[T3.11]** `[AC-2, AC-4, DR-9]` Implement `IsUserLectorAsync` and Lector exclusion filtering in `Backoffice/Guts.Infrastructure/Repositories/StudentCourseProfileDbRepository.cs`.
- [x] **[T3.12]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

### Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T3.13]** `[AC-4, US-5]` Write failing controller unit tests in `Backoffice/Guts.Api.Tests/Controllers/GamificationControllerTests.cs` for `GetHallOfFame` and `GetWeeklySprint` with Lector `perspectiveUserId` and `tier` query parameters vs. Student calls.
- [x] **[T3.14]** `[AC-4, US-5]` Update `GamificationController.cs` in `Backoffice/Guts.Api/Controllers/GamificationController.cs` to pass `isLector`, `perspectiveUserId`, and `tier` to `IGamificationService`.
- [x] **[T3.15]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

### Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T3.16]** `[AC-4, US-5]` Update Jasmine unit tests (`*.spec.ts`) in `Backoffice/Guts.Web/ClientApp/src/app/`:
  - `gamification.service.spec.ts`
  - `course-gamification.component.spec.ts` (verify Hero Summary is hidden for teachers, Student Perspective Selector is rendered for teachers)
  - `course-gamification-hall-of-fame.component.spec.ts` (verify 4-icon progression bar with active enlarged icon & blurred neighbors, overtake callout banner, and teacher tier switcher)
  - `course-gamification-sprint.component.spec.ts` (verify `perspectiveUserId` is passed when provided)
- [x] **[T3.17]** `[AC-4, US-5]` Implement updated models (`gamification.model.ts`), service methods (`gamification.service.ts`), and components (`course-gamification`, `course-gamification-hall-of-fame`, `course-gamification-sprint`).
- [x] **[T3.18]** Run `npm test -- --watch=false` in `Backoffice/Guts.Web/ClientApp` and verify all Karma/Jasmine tests pass.

---

### Phase 6: Final Verification & Quality Gate

- [x] **[T3.19]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [x] **[T3.20]** Run frontend production build: `npm run build` in `Backoffice/Guts.Web/ClientApp`
- [x] **[T3.21]** Update `spec.md` and `design.md` status to `Implemented`.
