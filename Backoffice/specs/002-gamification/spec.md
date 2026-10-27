# Specification: Course Gamification

- **Feature ID**: `002-gamification`
- **Status**: `Implemented` (Baseline Reference Spec)
- **Created**: `2026-10-27`

---

## 1. Context & Problem Statement

To motivate consistent practice, early assignment completion, and collaboration in programming courses, GUTS provides an opt-in **Course Gamification** subsystem per course and academic period.

When enabled by a Lector for a course (`CourseGamificationConfig`), students earn Experience Points (**XP**), level up through programming ranks, maintain ISO-week coding streaks (`WeeklyStreak`), unlock achievements (`BadgeType`), and compete on individual and class-group leaderboards (**Weekly Sprint**, **Hall of Fame**, and **Class Championship**), while retaining privacy controls (opt-out of leaderboards or display a pseudonym/avatar).

---

## 2. User Stories & Actors

- **Actors**: `Student`, `Lector (Teacher)`, `Background Scheduler (SprintLeaderboardBackgroundService)`

- **US-1 (Configure Course Gamification & Class Groups)**: As a **Lector**, I want to toggle gamification and its individual sub-features (Weekly Streaks, Leaderboards, Class Competition, Team Synergy, Badges) per course, and manage `CourseClassGroup` entities so I can tailor the experience to my course structure.
- **US-2 (Manage Student Profile & Privacy)**: As a **Student**, I want to customize my avatar, pseudonym, class group membership, and leaderboard visibility (`ShowInLeaderBoard`, `UsePseudonym`) so I can participate comfortably.
- **US-3 (Earn XP, Levels, Streaks, and Badges from Test Runs)**: As a **Student**, I want to automatically earn XP, level progression, weekly streak increments, and badges when my IDE submits passing test runs (`TestRun`) that yield new XP for exercises and project components.
- **US-4 (Review Recent XP Gains)**: As a **Student**, I want to see a concise breakdown of XP events earned since my last visit to the gamification dashboard (`RecentXpGainDto`), where all passing test events are summarized into a single row (`"Made {count} test(s) green"`) while bonus events are listed individually.
- **US-5 (Compete on Leaderboards)**: As a **Student** or **Lector**, I want to view the **Weekly Sprint Leaderboard** (current ISO week XP), **Hall of Fame Leaderboard** (all-time course XP), and **Class Championship Leaderboard** (average XP per class group).
- **US-6 (Automated Weekly Sprint Badge Awarding)**: As the **Background Scheduler**, I want to evaluate the previous ISO week's Sprint Leaderboard every Monday at `00:01 UTC` (using a Friday `23:59:59 UTC` cutoff) and award top-5 sprint badges (`SprintGold` through `SprintTop5`).
- **US-7 (Weekly Streak Loss & All-Tests-Passed Protection)**: As a **Student**, I want my weekly coding streak (`CurrentStreak`) to be lost if I earn no XP in the last ISO week, **except** when I have already passed all currently available tests in the course, in which case my streak is kept so I am not penalized for having no remaining tests to solve.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Course Configuration & Feature Toggles
- **Given** a course (`CourseId > 0`)
- **When** the course gamification configuration is requested and none exists yet
- **Then** a default `CourseGamificationConfig` is created with `IsGamificationEnabled = false` and all sub-toggles (`IsWeeklyStreakEnabled`, `IsLeaderboardEnabled`, `IsClassCompetitionEnabled`, `IsTeamSynergyEnabled`, `IsBadgesEnabled`) set to `true`.
- **When** a Lector updates the configuration
- **Then** the feature flags are persisted and govern subsequent test-run processing and leaderboard visibility.

### AC-2: Processing Test Runs for XP, Streaks & Completion Bonuses
- **Given** a newly registered `TestRun` with at least one passing `TestResult` in a course where `IsGamificationEnabled == true`
- **When** `GamificationService.ProcessTestRunAsync` executes
- **Then**:
  1. **First-time Passing Test (`+10 XP`)**: For each passing `TestId` not previously recorded as a `PassedTest` event on the student's `StudentCourseProfile`, `10 XP` is awarded.
  2. **Weekly Streak Update**: If `IsWeeklyStreakEnabled == true`, the student's `WeeklyStreak` is updated for the ISO year/week of `testRun.CreateDateTime`.
  3. **Assignment Completion Bonus (`+50 XP` + Speed Bonus)**: If the student has now passed **all** tests of the `Assignment` (and has not yet received `AssignmentCompleted` for it), `50 XP` is awarded. Additionally, based on `previousCompletionsCount` in the same course and period:
     - 1st completion (`0` previous): `+50 XP`
     - 2nd completion (`1` previous): `+40 XP`
     - 3rd completion (`2` previous): `+30 XP`
     - 4th–10th completion (`3..9` previous): `+20 XP`
     - 11th+ completion: `+0 XP`
     If a quick bonus (`> 0 XP`) is awarded, an `AssignmentCompletedQuickly` event is logged and the `SpeedDemon` badge is awarded (if badges are enabled).
  4. **Chapter / Topic Completion Bonus (`+100 XP`)**: If the student has now passed **all** tests across **all** assignments in the `Topic` (and has not yet received `ChapterCompleted` for that topic), `100 XP` is awarded along with the `CleanSweep` badge.
  5. **Team Synergy Bonus (`+30 XP`)**: If `IsTeamSynergyEnabled == true` and the student belongs to a `ProjectTeam` for the project topic where:
     - Every team member has passed at least one test of the component (`Overlaps`), AND
     - The union of passing tests across all team members covers 100% of the component's tests (`IsSubsetOf`),
     then every team member who has not yet received `TeamSynergy` for that component is awarded `+30 XP` and the `TeamPowerhouse` badge.

### AC-3: Badge Unlock Conditions
- **Given** `IsBadgesEnabled == true` for the course
- **When** a qualifying action occurs for a badge the student does not already own
- **Then** a `StudentBadge` is added once per `BadgeType`:
  - `FirstSpark` (`1`): First passing test in the course.
  - `SpeedDemon` (`2`): Top-10 fastest completion of all tests in an assignment.
  - `CleanSweep` (`4`): Passed 100% of tests across all assignments in a chapter/topic.
  - `NightOwl` (`5`): Passing test submitted between `00:00` and `04:59` (`Hour >= 0 && Hour < 5`).
  - `EarlyBird` (`6`): Passing test submitted between `05:00` and `07:59` (`Hour >= 5 && Hour < 8`).
  - `WeekendWarrior` (`7`): Passing test submitted on `Saturday` or `Sunday`.
  - `CenturyClub` (`8`): Reached $\ge 100$ unique `PassedTest` events in the course.
  - `OnFire` (`9`): Reached a `CurrentStreak` of $\ge 4$ consecutive ISO weeks.
  - `IronWill` (`10`): Reached a `CurrentStreak` of $\ge 8$ consecutive ISO weeks.
  - `TeamPowerhouse` (`12`): Earned the Team Synergy bonus on a project component.
  - `SprintGold` (`11`), `SprintSilver` (`13`), `SprintBronze` (`14`), `SprintTop4` (`15`), `SprintTop5` (`16`): Cumulative top-5 placement on the Weekly Sprint Leaderboard as of Friday `23:59:59 UTC` (e.g., Rank #1 earns Gold, Silver, Bronze, Top4, and Top5 if not already earned; ties with #1 on both `Score` and `GreenTestsCount` also earn `SprintGold`).

### AC-4: Leaderboards & Privacy Filtering
- **Given** opted-in student profiles (`ShowInLeaderBoard == true`) for a `(courseId, periodId)`
- **When** a leaderboard is requested:
  - **Weekly Sprint (`leaderboard/weekly`)**: Ranks students by XP earned between Monday `00:00` of the current ISO week and now (`Score`), tie-breaking by weekly `PassedTest` count (`GreenTestsCount`). Returns empty if `IsGamificationEnabled`, `IsWeeklyStreakEnabled`, or `IsLeaderboardEnabled` is `false`.
  - **Hall of Fame (`leaderboard/hall-of-fame`)**: Ranks students by all-time `TotalXp` descending, tie-breaking by total `PassedTest` count descending. Returns empty if `IsGamificationEnabled` or `IsLeaderboardEnabled` is `false`.
  - **Class Championship (`leaderboard/classes`)**: Aggregates opted-in students per `CourseClassGroup` and ranks class groups by `AverageXp` (rounded to 1 decimal place) descending, tie-breaking by `TotalGreenTests` descending. Returns empty if `IsGamificationEnabled` or `IsClassCompetitionEnabled` is `false`.
- **And** for each student entry, `DisplayName` uses `Pseudonym` when `UsePseudonym == true` (or when full name is blank), and `"FirstName LastName"` otherwise.

### AC-5: Profile Visit Tracking & Recent XP Gains
- **Given** a student viewing their gamification profile (`GET /api/courses/{courseId}/gamification/profile`)
- **When** the profile is loaded
- **Then** `RecentGains` summarizes all `GamificationEvent` items with `CreatedAt > LastLeaderboardVisitDate` (or all events on first visit):
  - All `GamificationEventType.PassedTest` events in the window are aggregated into a **single** `RecentXpEventDto` with:
    - `Description`: `"Made 1 test green"` when 1 test was passed, or `"Made {count} tests green"` when multiple tests were passed.
    - `XpEarned`: sum of `XpEarned` across all recent `PassedTest` events (e.g., `100` for 10 passed tests).
    - `CreatedAt`: the latest `CreatedAt` timestamp among the aggregated `PassedTest` events.
  - All non-`PassedTest` bonus events (`AssignmentCompleted`, `AssignmentCompletedQuickly`, `ChapterCompleted`, `TeamSynergy`) remain individual `RecentXpEventDto` items.
  - The resulting `RecentGains.Events` list is ordered by `CreatedAt` descending.
- **And** `LastLeaderboardVisitDate` is updated to `DateTime.UtcNow` if more than 10 minutes have elapsed since the previous recorded visit.

### AC-6: Weekly Streak Loss & All-Tests-Passed Protection
- **Given** a student with an active `WeeklyStreak` (`CurrentStreak > 0`) in a course where `IsGamificationEnabled == true` and `IsWeeklyStreakEnabled == true`
- **When** the previous ISO week has elapsed and the student earned `0 XP` during that week:
  - **If** the student has **not** yet passed all available tests in the course (across all topics/assignments in the active period, or when no tests exist yet), **Then** the student loses their weekly streak (`CurrentStreak` is reset to `0`, while `BestStreak` is preserved).
  - **If** the student has already passed **all** available tests in the course (and the course has at least 1 test), **Then** the student's `CurrentStreak` is kept (preserved without resetting) so they can continue their streak once new tests are provisioned.

---

## 4. Domain Rules & Invariants

- **DR-1 (Level Formula & Titles)**:
  $$\text{Level} = \lfloor \sqrt{\text{TotalXp} / 25.0} \rfloor + 1$$
  Titles map from Level 1 (`"Byte Novice"`), 2 (`"Syntax Scout"`), 3 (`"Logic Apprentice"`), 4 (`"Code Crafter"`), 5 (`"Algorithm Ace"`), 6 (`"Refactor Ranger"`), 7 (`"System Architect"`), 8 (`"Bug Whisperer"`), 9 (`"Full-Stack Guru"`), to Level 10+ (`"Code Titan"`).
- **DR-2 (Idempotent XP & Badges)**: Each `(GamificationEventType, TargetId)` combination and each `BadgeType` can be awarded **at most once** per `StudentCourseProfile`.
- **DR-3 (`WeeklyStreak` ISO-Week Calculation & Expiration)**:
  - Earning XP in the same ISO year & week leaves the streak unchanged.
  - Earning XP in the immediately following ISO week (`weeksDiff == 1`) increments `CurrentStreak` by 1 and updates `BestStreak = Math.Max(BestStreak, CurrentStreak)`.
  - If no XP is earned in the last ISO week (`weeksDiff > 1`), the streak is lost (`CurrentStreak` resets to `0`, or becomes `1` upon earning new XP in a later week) while preserving `BestStreak` — **unless** the student has already passed 100% of all currently available tests in the course, in which case `CurrentStreak` is kept.
  - `BestStreak` must always be $\ge$ `CurrentStreak`, and all values must be non-negative.
- **DR-4 (Profile Enrollment Defaults)**: Default `Pseudonym` is `"Student_{UserId}"` (if blank/omitted) and default `Avatar` is `"CodeCat"` (if blank/omitted).
- **DR-5 (Unique Class Group Names)**: Within a `(courseId, periodId)`, `CourseClassGroup.Name` cannot be empty/whitespace and must be unique.
- **DR-6 (Resilient Test Run Processing)**: Failures inside `GamificationService.ProcessTestRunAsync` are caught and logged so they never block or fail student test-run registration in `TestRunService.RegisterRunAsync`.

---

## 5. Authorization & Security Rules

- **All Gamification Endpoints (`/api/courses/{courseId}/gamification/...`)**: Require authentication (`[Authorize]`).
- **Students**:
  - Can view their own profile (`GET profile`) and enroll/update their own preferences (`POST enroll`).
  - Can view leaderboards (`GET leaderboard/weekly`, `GET leaderboard/hall-of-fame`, `GET leaderboard/classes`), course config (`GET config`), class groups (`GET classes`), and badge metadata (`GET badges`).
- **Lectors (`[Authorize(Roles = Role.Constants.Lector)]`)**:
  - Exclusive permission to update course gamification configuration (`PUT config`).
  - Exclusive permission to create (`POST classes`) and delete (`DELETE classes/{classGroupId}`) course class groups.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Duplicate or empty `CourseClassGroup` name | Throws `ContractException` | `400 Bad Request` |
| Deleting a `CourseClassGroup` belonging to a different `courseId` | Throws `DataNotFoundException` | `404 Not Found` |
| Rapidly refreshing profile within 10 minutes | `RecordLeaderboardVisit` returns `false`; `LastLeaderboardVisitDate` is not overwritten so recent XP banner stays visible for 10 minutes | `200 OK` |
| Exception thrown during `ProcessTestRunAsync` | Logged via `ILogger<GamificationService>`; `TestRun` registration still succeeds | `200 OK` |

---

## 7. Out of Scope

- Cross-course global leaderboards (all profiles, XP, and badges are scoped to `(UserId, CourseId, PeriodId)`).
- Manual XP adjustments by lectors.

---

## 8. Open Questions

- None (Baseline specification of existing production feature).

