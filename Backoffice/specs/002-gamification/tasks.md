# TDD Implementation Plan: Course Gamification

- **Feature ID**: `002-gamification`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Increment 1: Summarize Passed Test Events in Recent XP Gains (US-4 / AC-5) — _Completed_

- [x] **[T0.1]** `[US-4, AC-5]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/GamificationServiceTests.cs` (`GetStudentCourseProfileAsync_WithMultiplePassedTests_ShouldSummarizePassedTestsIntoSingleEvent`, `GetStudentCourseProfileAsync_WithSinglePassedTest_ShouldUseSingularDescription`).
- [x] **[T0.2]** `[US-4, AC-5]` Update `GamificationService.GetStudentCourseProfileAsync` in `Backoffice/Guts.Application/Services/GamificationService.cs` to aggregate `GamificationEventType.PassedTest` events in `RecentGains.Events`.
- [x] **[T0.3]** `[US-4, AC-5]` Update unit test in `Backoffice/Guts.Web/ClientApp/src/app/components/course-gamification/course-gamification-hero.component.spec.ts` and verify all backend & frontend tests pass.

---

## Increment 2: Weekly Streak Loss & All-Tests-Passed Protection (US-7 / AC-6 / DR-3) — _Completed_

- [x] **[T1.1]** `[US-7, AC-6, DR-3]` Write and pass unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/WeeklyStreakTests.cs` for `NeedsEvaluation` and `EvaluateStreak`.
- [x] **[T1.2]** `[US-7, AC-6, DR-3]` Write and pass unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/StudentCourseProfileTests.cs` for `NeedsWeeklyStreakEvaluation` and `EvaluateWeeklyStreak`.
- [x] **[T1.3]** `[US-7, AC-6, DR-3]` Implement `NeedsEvaluation` and `EvaluateStreak` in `WeeklyStreak.cs` and `StudentCourseProfile.cs`.
- [x] **[T2.1]** `[US-7, AC-6]` Update repository contracts (`IAssignmentRepository.GetCourseTestIdsAsync`, `IStudentCourseProfileRepository.GetByCourseAndPeriodAsync`, `UpdateRangeAsync`).
- [x] **[T2.2]** `[US-7, AC-6, DR-3]` Write and pass unit tests in `GamificationServiceTests.cs` and implement lazy + batch streak evaluation in `GamificationService.cs`.
- [x] **[T3.1]** `[US-7, AC-6]` Implement and test repository methods in `AssignmentDbRepository.cs` and `StudentCourseProfileDbRepository.cs`.

---

## Increment 3: Tiered Hall of Fame, Overtake/Tier-Up Bonuses, Teacher Exclusion & Student Perspective (US-3 / US-4 / US-5 / AC-2 / AC-4 / AC-5 / DR-7 / DR-8 / DR-9) — _Completed_

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
  - Verify `HallOfFameLeague.GetVisibleTierWindow(HallOfFameTier currentTier)` returns a 4-tier progression window `[Previous2, Previous1, Current, Next1]` showing only the next tier to reach (unless in the two lowest levels `Paper` and `Wood`), and clamps to 4 tiers at boundaries (`[Paper, Wood, Iron, Bronze]` at `Paper`, `Wood`, and `Iron`; `[Silver, Gold, Diamond, Obsidian]` at `Diamond` and `Obsidian`), and `GamificationService` filters `VisibleTiers` to tiers with `StudentCount > 0`.
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

### Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T3.5]** `[AC-2, AC-4, DR-9]` Update `IStudentCourseProfileRepository` (`IsUserLectorAsync`), `IGamificationService` (`GetHallOfFameLeaderboardAsync` returning `HallOfFameLeaderboardDto`), and DTOs in `Backoffice/Guts.Application/Dtos/Gamification/GamificationDtos.cs`.
- [x] **[T3.6]** `[AC-2, AC-4, AC-5, DR-7, DR-8, DR-9]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/GamificationServiceTests.cs`:
  - `ProcessTestRunAsync_WhenUserIsLector_ShouldSkipGamificationProcessing` (`[AC-2, DR-9]`).
  - `ProcessTestRunAsync_WhenStudentOvertakesAndRisesTier_ShouldAwardOvertakeAndTierUpBonusXp` (`[AC-2, DR-8]`).
  - `GetHallOfFameLeaderboardAsync_WithEqualTotalXp_ShouldRankStudentWhoReachedXpFirstHigher` (`[AC-4, DR-7]`).
  - `GetHallOfFameLeaderboardAsync_ForStudent_ShouldReturnOnlyCurrentTierEntriesVisibleTiersAndNextTarget` (`[AC-4, DR-7]`).
  - `GetHallOfFameLeaderboardAsync_ForLector_ShouldReturnAllLevelsOrSelectedStudentPerspective` (`[AC-4, US-5]`).
  - `GetStudentCourseProfileAsync_WithOvertakeAndTierUpEvents_ShouldIncludeInRecentGains` (`[AC-5]`).
- [x] **[T3.7]** `[AC-2, AC-4, AC-5, DR-7, DR-8, DR-9]` Implement teacher exclusion, Hall of Fame tie-breaking, tiered leaderboard slicing, 4-icon window, next target calculation, student perspective support, and overtake/tier-up evaluation in `Backoffice/Guts.Application/Services/GamificationService.cs`.
- [x] **[T3.8]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

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

---

## Increment 4: Teacher Student Perspective Inspection — Hero Summary, Badges & Leaderboards (US-5 / AC-4 / AC-5)

### Phase 1: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T4.1]** `[US-5, AC-4, AC-5]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/GamificationServiceTests.cs`:
  - `GetStudentCourseProfileAsync_WhenUserIsLectorWithPerspectiveUserId_ShouldReturnTargetStudentProfileWithoutUpdatingLastVisitDate`
  - `GetStudentCourseProfileAsync_WhenUserIsStudentWithPerspectiveUserId_ShouldIgnorePerspectiveUserIdAndReturnOwnProfile`
- [x] **[T4.2]** `[US-5, AC-4, AC-5]` Update `IGamificationService` and `GamificationService.GetStudentCourseProfileAsync` in `Backoffice/Guts.Application/Services/GamificationService.cs`:
  - Accept `bool isLector = false` and `int? perspectiveUserId = null`.
  - Resolve target user ID: `int targetUserId = isLector && perspectiveUserId.HasValue ? perspectiveUserId.Value : userId;`.
  - When `isLector && perspectiveUserId.HasValue`, do not invoke `profile.RecordLeaderboardVisit()` (preserving the student's `LastLeaderboardVisitDate`).
- [x] **[T4.3]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

### Phase 2: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.4]** `[US-5, AC-4, AC-5]` Write failing controller unit tests in `Backoffice/Guts.Api.Tests/Controllers/GamificationControllerTests.cs`:
  - `GetProfile_WhenCalledByLectorWithPerspectiveUserId_ShouldPassPerspectiveUserIdAndLectorFlagToService`
  - `GetProfile_WhenCalledByStudentWithPerspectiveUserId_ShouldPassNullPerspectiveUserIdToService`
- [x] **[T4.5]** `[US-5, AC-4, AC-5]` Update `GamificationController.GetProfile` in `Backoffice/Guts.Api/Controllers/GamificationController.cs` to accept `[FromQuery] int? perspectiveUserId = null` and pass `IsLector()` and `perspectiveUserId` to `gamificationService.GetStudentCourseProfileAsync`.
- [x] **[T4.6]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

### Phase 3: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T4.7]** `[US-5, AC-4, AC-5]` Update Jasmine unit tests (`*.spec.ts`):
  - In `gamification.service.spec.ts`: verify `getProfile` appends `perspectiveUserId` query parameter when provided.
  - In `course-gamification-hero.component.spec.ts`: verify that when `readOnly = true`, the "Customize Profile" / settings button is hidden and `openSettings()` is disabled.
  - In `course-gamification.component.spec.ts`:
    - When teacher selects a student (`perspectiveUserId = 42`), invokes `gamificationService.getProfile(courseId, periodId, 42)`, renders `<app-course-gamification-hero>` with `[profile]="profile"` and `[readOnly]="true"`, and passes `[profile]="profile"` to `<app-course-gamification-badges>`.
    - When teacher selects "All Levels" (`perspectiveUserId = null`), clears `profile` to `null` and hides the hero summary card.
- [x] **[T4.8]** `[US-5, AC-4, AC-5]` Implement frontend updates:
  - In `gamification.service.ts`: update `getProfile` signature to `(courseId: number, periodId?: number, perspectiveUserId?: number)` and append `perspectiveUserId` when defined.
  - In `course-gamification-hero.component.ts` & `.html`: add `@Input() public readOnly: boolean = false;`, hide the customize button when `readOnly`, and prevent opening settings.
  - In `course-gamification.component.ts` & `.html`:
    - Listen to `perspectiveUserId` changes to call `loadProfile(perspectiveUserId)` when set, or clear `profile` when null.
    - Render `<app-course-gamification-hero [profile]="profile" [readOnly]="userProfile.isTeacher()">` when `config?.isGamificationEnabled && profile && (!userProfile.isTeacher() || perspectiveUserId)`.
    - Pass `[profile]="profile"` to `<app-course-gamification-badges>` and display `profile?.earnedBadges?.length || 0` in the Badges tab header.
- [x] **[T4.9]** Run `npm test -- --watch=false` in `Backoffice/Guts.Web/ClientApp` and verify all Karma/Jasmine tests pass.

---

### Phase 4: Final Verification & Quality Gate

- [x] **[T4.10]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [x] **[T4.11]** Run frontend production build: `npm run build` in `Backoffice/Guts.Web/ClientApp`
- [x] **[T4.12]** Run SonarQube analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and resolve any reported bugs, code smells, or coverage gaps.
- [x] **[T4.13]** Update `spec.md`, `design.md`, and `tasks.md` status.

---

## Increment 5: Thursday Midday Streak Reminder Notifications & Throttled Queue (US-8 / AC-7 / DR-10 / DR-11 / DR-12)

### Phase 1: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T5.1]** `[US-8, AC-7]` Create `StreakReminderAssignmentDto` and `StreakReminderEmailMessage` in `Backoffice/Guts.Application/Dtos/Gamification/GamificationDtos.cs`.
- [x] **[T5.2]** `[US-8, AC-7]` Add `SendStreakReminderEmailAsync` to `IMailSender` and write unit tests in `Backoffice/Guts.Application.Tests/Communication/MailSenderTests.cs` (verifying subject, greeting, streak count, next target to beat, recommended assignment links). Implement in `Backoffice/Guts.Application/Communication/MailSender.cs`.
- [x] **[T5.3]** `[US-8, AC-7, DR-10, DR-11, DR-12]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/StreakReminderServiceTests.cs`:
  - `SendStreakRemindersAsync_WhenStudentHasStreakAndNoXpThisWeek_ShouldQueueReminder` (`[US-8, AC-7, DR-10]`).
  - `SendStreakRemindersAsync_WhenStudentEarnedXpThisWeek_ShouldNotQueueReminder` (`[DR-10]`).
  - `SendStreakRemindersAsync_WhenStudentCompletedAllTests_ShouldNotQueueReminder` (`[DR-10]`).
  - `SendStreakRemindersAsync_WhenUserIsLector_ShouldNotQueueReminder` (`[DR-10]`).
  - `SendStreakRemindersAsync_ShouldRecommendInProgressAssignmentsBeforeUntouchedAssignments` (`[DR-11]`).
  - `SendStreakRemindersAsync_WhenStudentOptedIn_ShouldIncludeNextTargetStudent` (`[AC-7]`).
  - `SendStreakRemindersAsync_WhenEmailFailsForOneStudent_ShouldLogAndContinueQueue` (`[AC-7, DR-12]`).
- [x] **[T5.4]** `[US-8, AC-7, DR-10, DR-11, DR-12]` Implement `IStreakReminderService` and `StreakReminderService` in `Backoffice/Guts.Application/Services/StreakReminderService.cs`.
- [x] **[T5.5]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

### Phase 2: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T5.6]** `[US-8, AC-7]` Write unit tests in `Backoffice/Guts.Api.Tests/BackgroundServices/StreakReminderBackgroundServiceTests.cs` verifying `GetNextThursdayMiddayTrigger` and execution loop.
- [x] **[T5.7]** `[US-8, AC-7]` Implement `StreakReminderBackgroundService` in `Backoffice/Guts.Api/BackgroundServices/StreakReminderBackgroundService.cs` and register it in `Backoffice/Guts.Api/Startup.cs`.
- [x] **[T5.8]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

### Phase 3: Final Verification & Quality Gate

- [x] **[T5.9]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [x] **[T5.10]** Run SonarQube analysis or quality checks.
- [x] **[T5.11]** Update `spec.md`, `design.md`, and `tasks.md` status to `Implemented`.

---

## Increment 6: Additional Streak, Test Count, and Assignment Completion Badges (AC-3 / DR-2) — _Completed_

### Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T6.1]** `[AC-3, DR-2]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/StudentCourseProfileTests.cs`:
  - Verify `RecordWeeklyActivity` / `AwardPassedTestXp` awards `BadgeType.WarmingUp` at a 2-week streak and `BadgeType.Unstoppable` at a 6-week streak (alongside `OnFire` at 4 weeks and `IronWill` at 8 weeks).
  - Verify `AwardPassedTestXp` awards `BadgeType.QuarterCentury` upon reaching 25 unique passing tests and `BadgeType.HalfCentury` upon reaching 50 unique passing tests.
  - Verify `AwardAssignmentBonus` awards `BadgeType.FirstVictory` upon completing the 1st assignment and `BadgeType.HighFive` upon completing 5 assignments (when `isBadgesEnabled == true`).
- [x] **[T6.2]** `[AC-3, DR-2]` Add `WarmingUp = 17`, `Unstoppable = 18`, `QuarterCentury = 19`, `HalfCentury = 20`, `FirstVictory = 21`, `HighFive = 22` to `BadgeType.cs` and implement the unlock rules in `StudentCourseProfile.cs`.
- [x] **[T6.3]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

### Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T6.4]** `[AC-3]` Write failing unit test in `Backoffice/Guts.Application.Tests/Services/GamificationServiceTests.cs` verifying `GetBadgeMetadataAsync` and `MapToProfileDto` include metadata (`Name`, `Description`, `Icon`) for all 21 badge types (including `WarmingUp`, `Unstoppable`, `QuarterCentury`, `HalfCentury`, `FirstVictory`, and `HighFive`).
- [x] **[T6.5]** `[AC-3]` Register the 6 new badges in `GamificationService.BadgeMetadata` in `Backoffice/Guts.Application/Services/GamificationService.cs`.
- [x] **[T6.6]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

### Phase 3: Final Verification & Quality Gate

- [x] **[T6.7]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [x] **[T6.8]** Update `tasks.md` checkboxes and verify `spec.md` and `design.md` status.

---

## Increment 7: Stepped-Bracket Class Championship Scoring & Personal Contribution (AC-4 / DR-13) — _Completed_

### Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T7.1]** `[AC-4, DR-13]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/CourseClassGroupTests.cs`:
  - Verify `GetContributionWeightForRank` returns `1.00` for ranks `1..5`, `0.60` for ranks `6..10`, `0.30` for ranks `11..15`, and `0.15` for ranks `>= 16`, and throws `ContractException` for `<= 0`.
  - Verify `CalculateStudentContribution` returns `5 + Round(totalXp * weight, MidpointRounding.AwayFromZero)` (including `5` when `totalXp == 0`).
  - Verify `CalculateClassScore` orders students by `TotalXp` descending, `LastXpEarnedAt` ascending, `Id` ascending, sums their contributions, never decreases when a `0 XP` or low-XP student joins, and returns `CurrentUserContribution` when `currentUserId` matches a class member.
- [x] **[T7.2]** `[AC-4, DR-13]` Implement `JoinBonusPoints`, `GetContributionWeightForRank`, `CalculateStudentContribution`, and `CalculateClassScore` in `Backoffice/Guts.Domain/GamificationAggregate/CourseClassGroup.cs`.
- [x] **[T7.3]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

### Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T7.4]** `[AC-4, DR-13]` Update `ClassGroupLeaderboardEntryDto` in `Backoffice/Guts.Application/Dtos/Gamification/GamificationDtos.cs` (replace `AverageXp` with `ClassScore`, `IsCurrentUserClass`, and `CurrentUserContribution`) and update `IGamificationService.GetClassGroupLeaderboardAsync`.
- [x] **[T7.5]** `[AC-4, DR-13]` Update and add unit tests in `Backoffice/Guts.Application.Tests/Services/GamificationServiceTests.cs`:
  - Verify `GetClassGroupLeaderboardAsync` uses `GetByCourseAndPeriodAsync` (including `ShowInLeaderBoard == false` students), calculates `ClassScore` via stepped brackets + join bonus, ranks by `ClassScore` descending then `TotalGreenTests` descending, and populates `IsCurrentUserClass` and `CurrentUserContribution` for `currentUserId`.
- [x] **[T7.6]** `[AC-4, DR-13]` Implement updated `GetClassGroupLeaderboardAsync` in `Backoffice/Guts.Application/Services/GamificationService.cs`.
- [x] **[T7.7]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

### Phase 3: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T7.8]** `[AC-4, DR-13]` Update `GamificationController.GetClassLeaderboard` in `Backoffice/Guts.Api/Controllers/GamificationController.cs` to accept `[FromQuery] int? perspectiveUserId = null` and pass `effectiveUserId` (`IsLector() ? perspectiveUserId : GetUserId()`) to `GetClassGroupLeaderboardAsync`, and update `Backoffice/Guts.Api.Tests/Controllers/GamificationControllerTests.cs`.
- [x] **[T7.9]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

### Phase 4: Frontend Layer (`Guts.Web/ClientApp`)

- [x] **[T7.10]** `[AC-4, DR-13]` Update `IClassGroupLeaderboardEntryModel` in `gamification.model.ts` (`classScore`, `isCurrentUserClass`, `currentUserContribution`) and `getClassLeaderboard(courseId, periodId?, perspectiveUserId?)` in `gamification.service.ts` + `gamification.service.spec.ts`.
- [x] **[T7.11]** `[AC-4, DR-13]` Update `CourseGamificationClassChampionshipComponent` (`course-gamification-class-championship.component.ts`, `.html`, `.spec.ts`) and `course-gamification.component.html`:
  - Pass `[isTeacher]="userProfile.isTeacher()"` and `[perspectiveUserId]="perspectiveUserId"` to `<app-course-gamification-class-championship>`.
  - Display personal contribution callout banner (in score points only, without exposing bracket weights), highlight the student's class card (`isCurrentUserClass`), and display `Class Score` (`classScore pts`) instead of `Average XP`.
- [x] **[T7.12]** Run `npm test -- --watch=false` in `Backoffice/Guts.Web/ClientApp` and verify all Karma/Jasmine tests pass.

---

### Phase 5: Final Verification

- [x] **[T7.13]** Run full backend test suite (`dotnet test Backoffice/Guts.slnx`) and mark tasks complete.

---

## Increment 8: Daily Overtake Bonus Cap (+4 / +6 XP per UTC Day) & Weekly Hall of Fame Rank Defense Bonus (+5 XP & Rank Defender Badge) (US-3 / US-6 / AC-2 / AC-3 / AC-5 / AC-8 / DR-8) — _Completed_

### Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T8.1]** `[AC-2, DR-8]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/StudentCourseProfileTests.cs` for the daily overtake cap in `EvaluateHallOfFameProgression`:
  - Verify overtaking 1 student awards `+4 XP`.
  - Verify overtaking 2 or more students (e.g. 50 students) in a single test run awards `+6 XP` total (`+4 XP` for the 1st surpassed rank, `+2 XP` for the 2nd, and `0 XP` for the remaining 48), while updating `BestHallOfFameRank` to `newRank` and recording `HallOfFameOvertake` events for all surpassed ranks.
  - Verify overtaking 1 student in the morning (`+4 XP`) and 1+ more students later on the same UTC day tops up `+2 XP` to the `6 XP` daily maximum, and any further overtakes on the same UTC day award `0 XP`.
  - Verify overtaking students on a subsequent UTC day resets the daily allowance (`+4 XP` / `+6 XP`).
- [x] **[T8.2]** `[AC-3, AC-8, DR-8]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/GamificationAggregate/StudentCourseProfileTests.cs` for `EvaluateWeeklyRankDefense`:
  - Awards `+5 XP` (`GamificationEventType.HallOfFameRankDefended`, `TargetId = year * 100 + weekNumber`, `CreatedAt = endOfWeek`) when the student is opted in (`ShowInLeaderBoard == true`), has `TotalXp > 0`, held or improved their `BestHallOfFameRank` (`currentRank <= BestHallOfFameRank.Value`), is either Rank #1 or has at least 1 ranked student behind them (`currentRank < totalRankedStudents`), and was active in the week (or passed 100% of available course tests).
  - Does not award the bonus if the student was overtaken (`currentRank > BestHallOfFameRank.Value`), is in last place (`currentRank == totalRankedStudents && currentRank > 1`), was inactive without 100% tests passed, or already received the bonus for `(year, weekNumber)`.
  - Awards `BadgeType.RankDefender` (`23`) upon reaching 3 defended weeks when `isBadgesEnabled == true`.
- [x] **[T8.3]** `[AC-2, AC-3, AC-8, DR-8]` Update `GamificationEventType.cs` (`HallOfFameRankDefended = 8`), `BadgeType.cs` (`RankDefender = 23`), and `StudentCourseProfile.cs` (`ApplyOvertakeProgression` daily cap and `EvaluateWeeklyRankDefense`).
- [x] **[T8.4]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

### Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T8.5]** `[AC-3, AC-5, AC-8]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/GamificationServiceTests.cs`:
  - Verify `GetBadgeMetadata` includes `BadgeType.RankDefender` (`"Rank Defender"`, `🏰`).
  - Verify `GetStudentCourseProfileAsync` summarizes `HallOfFameOvertake` events only when summed `XpEarned > 0` and maps `HallOfFameRankDefended` events to `"Hall of Fame rank defense bonus"`.
  - Verify `AwardWeeklySprintBadgesAsync` evaluates weekly Hall of Fame rank defense for opted-in students with `TotalXp > 0` and persists modified profiles.
- [x] **[T8.6]** `[AC-3, AC-5, AC-8]` Update `GamificationService.cs` (`BadgeMetadata`, event summarization via `RecentGainsSummary`, and `EvaluateWeeklyRankDefenseForCourseAsync` called during `AwardWeeklySprintBadgesAsync`).
- [x] **[T8.7]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

### Phase 3: Frontend Layer (`Guts.Web/ClientApp`)

- [x] **[T8.8]** `[US-3, AC-4]` Update `course-gamification-hall-of-fame.component.html` and `course-gamification-hall-of-fame.component.spec.ts` to reflect the daily overtake bonus (`+4 XP` for 1 student, `+6 XP` for 2+ students, max `6 XP/day`).
- [x] **[T8.9]** Run `npm test -- --watch=false` in `Backoffice/Guts.Web/ClientApp` and verify all Karma/Jasmine tests pass.

---

### Phase 4: Final Verification

- [x] **[T8.10]** Run full backend test suite (`dotnet test Backoffice/Guts.slnx`) and mark tasks complete.
