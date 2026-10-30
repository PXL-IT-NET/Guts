# Specification: Course Gamification

- **Feature ID**: `002-gamification`
- **Status**: `Implemented`
- **Created**: `2026-10-27`
- **Updated**: `2026-10-30`

---

## 1. Context & Problem Statement

To motivate consistent practice, early assignment completion, and collaboration in programming courses, GUTS provides an opt-in **Course Gamification** subsystem per course and academic period.

When enabled by a Lector for a course (`CourseGamificationConfig`), students earn Experience Points (**XP**), level up through programming ranks, maintain ISO-week coding streaks (`WeeklyStreak`), unlock achievements (`BadgeType`), and compete on individual and class-group leaderboards (**Weekly Sprint**, **Hall of Fame**, and **Class Championship**), while retaining privacy controls (opt-out of leaderboards or display a pseudonym/avatar).

To prevent demotivation among students with lower rankings on the **Hall of Fame Leaderboard**, the Hall of Fame is divided into **8 League Tiers** (**Obsidian**, **Diamond**, **Gold**, **Silver**, **Bronze**, **Iron**, **Wood**, and **Paper**). In the UI, rankings within every league tier **always start at 1** (awarding 🥇 gold, 🥈 silver, 🥉 bronze to the top 3 of that tier, followed by `#4`, `#5`, etc.), no matter the tier. Global rankings across the entire course are **never** displayed in the UI to students or teachers to avoid demotivating lower ranked students. Students only see the leaderboard slice for their own current tier, are visually guided by a 4-icon tier progression bar, and are actively incentivized to overtake the student directly above them (`+4 XP` per newly surpassed rank) and climb to higher tiers (`+5 XP` per newly reached tier).

Teachers (`Lector`) are completely excluded from all rankings and do not have a personal Hero Summary card. Instead, teachers can view all Hall of Fame levels or select a specific student to inspect the leaderboards from that student's exact perspective.

---

## 2. User Stories & Actors

- **Actors**: `Student`, `Lector (Teacher)`, `Background Scheduler (SprintLeaderboardBackgroundService)`

- **US-1 (Configure Course Gamification & Class Groups)**: As a **Lector**, I want to toggle gamification and its individual sub-features (Weekly Streaks, Leaderboards, Class Competition, Team Synergy, Badges) per course, and manage `CourseClassGroup` entities so I can tailor the experience to my course structure.
- **US-2 (Manage Student Profile & Privacy)**: As a **Student**, I want to customize my avatar, pseudonym, class group membership, and leaderboard visibility (`ShowInLeaderBoard`, `UsePseudonym`) so I can participate comfortably.
- **US-3 (Earn XP, Levels, Streaks, Badges, and Ranking Bonuses from Test Runs)**: As a **Student**, I want to automatically earn XP, level progression, weekly streak increments, badges, and Hall of Fame overtake (`+4 XP`) and tier-promotion (`+5 XP`) bonuses when my IDE submits passing test runs (`TestRun`) that yield new XP for exercises and project components.
- **US-4 (Review Recent XP Gains)**: As a **Student**, I want to see a concise breakdown of XP events earned since my last visit to the gamification dashboard (`RecentXpGainDto`), where all passing test events are summarized into a single row (`"Made {count} test(s) green"`) while bonus events (including overtake and tier-up bonuses) are listed individually.
- **US-5 (Compete on Tiered Hall of Fame & Other Leaderboards)**:
  - As a **Student**, I want to view the **Weekly Sprint Leaderboard**, the **Class Championship Leaderboard**, and **only my current league tier** of the **Hall of Fame Leaderboard** where rankings always start at #1 (with gold/silver/bronze medals for the top 3 in my tier and no demotivating global rank displayed), alongside a 4-icon tier progression header (1 previous, current enlarged, 2 next) and a motivational callout to overtake the student directly above me.
  - As a **Lector (Teacher)**, I want to be excluded from all rankings and not have a personal Hero Summary; instead, I want to be able to see **all Hall of Fame levels** with tier-relative ranks (no global ranks) AND **select a student** to view the leaderboards from that student's perspective.
- **US-6 (Automated Weekly Sprint Badge Awarding)**: As the **Background Scheduler**, I want to evaluate the previous ISO week's Sprint Leaderboard every Monday at `00:01 UTC` (using a Friday `23:59:59 UTC` cutoff) and award top-5 sprint badges (`SprintGold` through `SprintTop5`) to eligible students.
- **US-7 (Weekly Streak Loss & All-Tests-Passed Protection)**: As a **Student**, I want my weekly coding streak (`CurrentStreak`) to be lost if I earn no XP in the last ISO week, **except** when I have already passed all currently available tests in the course, in which case my streak is kept so I am not penalized for having no remaining tests to solve.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Course Configuration & Feature Toggles
- **Given** a course (`CourseId > 0`)
- **When** the course gamification configuration is requested and none exists yet
- **Then** a default `CourseGamificationConfig` is created with `IsGamificationEnabled = false` and all sub-toggles (`IsWeeklyStreakEnabled`, `IsLeaderboardEnabled`, `IsClassCompetitionEnabled`, `IsTeamSynergyEnabled`, `IsBadgesEnabled`) set to `true`.
- **When** a Lector updates the configuration
- **Then** the feature flags are persisted and govern subsequent test-run processing and leaderboard visibility.

### AC-2: Processing Test Runs for XP, Streaks, Completion Bonuses & Ranking Bonuses (Students Only)
- **Given** a newly registered `TestRun` with at least one passing `TestResult` in a course where `IsGamificationEnabled == true`
- **When** `GamificationService.ProcessTestRunAsync` executes:
  - **If** the user who submitted the `TestRun` is a teacher (`Lector` role), processing exits immediately without creating a `StudentCourseProfile` or awarding XP, so teachers are never included in gamification rankings or completion counts.
  - **If** the user is a student (`Student` role), **Then**:
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
    6. **Hall of Fame Overtake Bonus (`+4 XP` per newly surpassed rank) & Tier Promotion Bonus (`+5 XP` per newly reached tier)**:
       - If `IsLeaderboardEnabled == true`, the student's profile is opted in (`ShowInLeaderBoard == true`), and the student earned new XP during steps 1–5:
       - The Hall of Fame ranking of opted-in students for `(courseId, periodId)` is evaluated before and after the student's XP gain from steps 1–5 (using the tie-breaking rule in `DR-7`).
       - **Overtake Bonus (`+4 XP`)**: For each rank position the student improves beyond their previously recorded best Hall of Fame rank (`BestHallOfFameRank`, initialized to their rank prior to the overtake among currently ranked students), the student is awarded `+4 XP` per newly surpassed position (`GamificationEventType.HallOfFameOvertake`, `TargetId = newRank`).
       - **Tier Promotion Bonus (`+5 XP`)**: If the student's new Hall of Fame rank places them in a higher Hall of Fame League Tier (`HallOfFameTier`) than their best previously achieved tier (`BestHallOfFameTier`), the student is awarded `+5 XP` for each newly reached tier (`GamificationEventType.HallOfFameTierUp`, `TargetId = (int)tier`).
       - Awarding Overtake and Tier Promotion XP updates the student's `TotalXp` at the end of the test run without triggering recursive secondary overtake evaluations.

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

### AC-4: Leaderboards, Hall of Fame Tiers, Teacher Exclusion & Student Perspective
- **Given** opted-in student profiles (`ShowInLeaderBoard == true`, excluding any user with the `Lector` role) for a `(courseId, periodId)`
- **When** a leaderboard is requested:
  - **Teacher Exclusion**: Users with the `Lector` role are excluded from all rankings (**Weekly Sprint**, **Hall of Fame**, and **Class Championship**).
  - **Weekly Sprint (`leaderboard/weekly`)**: Ranks students who have earned XP (`Score > 0`) between Monday `00:00` of the current ISO week and now by `Score` descending, tie-breaking by weekly `PassedTest` count (`GreenTestsCount`). Students with `0 XP` for the current week are excluded from the Weekly Sprint Leaderboard. In the UI, when a student (or a Lector viewing a selected student's perspective) is not on the Weekly Sprint Leaderboard, a callout banner triggers the student to earn XP to get on the board. If a Lector passes a `perspectiveUserId` query parameter, `IsCurrentUser` highlights that selected student. Returns empty if `IsGamificationEnabled`, `IsWeeklyStreakEnabled`, or `IsLeaderboardEnabled` is `false`.
  - **Hall of Fame (`leaderboard/hall-of-fame`)**:
    - Ranks opted-in students by all-time `TotalXp` descending.
    - **Tie-Breaking Rule**: When two or more students have equal `TotalXp`, the student who reached that XP level **first** (earliest timestamp of their most recent XP-earning `GamificationEvent`, or earliest `Id` if `TotalXp == 0`) is ranked higher.
    - Assigns each student to one of the **8 Hall of Fame League Tiers** based on their global standing (see `DR-7`).
    - **Tier-Relative Ranks (Starts at 1, No Global Rank Displayed)**: In the UI and returned entries, ranking in a league tier **always starts at 1** (`🥇 1`, `🥈 2`, `🥉 3`, `#4`, ...) for every tier. Global ranks across the whole course are **never** displayed in the UI for students or teachers to avoid demotivating students in lower tiers.
    - **Student View (or Lector with a selected student `perspectiveUserId`)**: Returns only the leaderboard entries belonging to the target student's current Hall of Fame tier (if opted out or unranked, defaults to the lowest active tier / `Paper`), along with:
      - `CurrentTier`: metadata (tier enum/id, name, icon) of the displayed tier. Note: Tier position/rank ranges (e.g., `(#1–#15)`) must **not** be displayed in the UI (e.g., display `"Obsidian League"` rather than `"Obsidian League (#1–#15)"`).
      - `CurrentUserRank`: the student's 1-based relative rank within their tier (`1` for the top student in their tier).
      - `VisibleTiers`: the 4-tier progression window around the student's current tier (normally `[PreviousTier, CurrentTier, NextTier1, NextTier2]`; clamped to a 4-tier window at the boundaries: `[Paper, Wood, Iron, Bronze]` when at `Paper`, and `[Silver, Gold, Diamond, Obsidian]` when at `Diamond` or `Obsidian`), flagging which tier is active so the UI renders the current tier icon larger and the other 3 icons smaller and slightly blurred.
      - `NextTargetStudent`: information about the student immediately above the target student in overall course order (even if that student is at the bottom of the next tier above), with `Rank` expressing their relative rank within their own tier (e.g., `#15 in 🌋 Obsidian`), including their `DisplayName`, `Avatar`, `Score`, and `XpNeededToOvertake` (`(targetScore - currentScore) + 1`, or `1` when tied on XP), to trigger and motivate the student to overtake them.
    - **Lector (Teacher) Overview & Student Perspective Selector**:
      - Teachers do **not** have a personal Hero Summary card (`app-course-gamification-hero` is hidden for teachers).
      - Instead, at the top of the Gamification page, teachers see a **"Student Perspective" selector** allowing them to:
        1. **View All Levels (Default Teacher Mode)**: Browse and switch between all 8 Hall of Fame tiers (or view all ranked students across all tiers), with each tier displaying tier-relative ranks (starting at 1) without global ranks.
        2. **Select a Specific Student**: View the leaderboards (Hall of Fame tier slice with tier-relative ranks, 4-icon progression header, overtake target callout, and Weekly Sprint highlight) from that selected student's exact perspective.
      - `StudentOptions`: populates the teacher dropdown with ranked students showing their tier-relative rank and tier (e.g., `#1 Alice (💎 Diamond — 180 XP)`), completely avoiding global ranks.
    - Returns empty if `IsGamificationEnabled` or `IsLeaderboardEnabled` is `false`.
  - **Class Championship (`leaderboard/classes`)**: Aggregates opted-in students (excluding Lectors) per `CourseClassGroup` and ranks class groups by `AverageXp` (rounded to 1 decimal place) descending, tie-breaking by `TotalGreenTests` descending. Returns empty if `IsGamificationEnabled` or `IsClassCompetitionEnabled` is `false`.
- **And** for each student entry, `DisplayName` uses `Pseudonym` when `UsePseudonym == true` (or when full name is blank), and `"FirstName LastName"` otherwise. (For Lectors populating the Student Selector, students are listed so the teacher can easily select any ranked student).

### AC-5: Profile Visit Tracking & Recent XP Gains
- **Given** a student viewing their gamification profile (`GET /api/courses/{courseId}/gamification/profile`)
- **When** the profile is loaded
- **Then** `RecentGains` summarizes all `GamificationEvent` items with `CreatedAt > LastLeaderboardVisitDate` (or all events on first visit):
  - All `GamificationEventType.PassedTest` events in the window are aggregated into a **single** `RecentXpEventDto` with:
    - `Description`: `"Made 1 test green"` when 1 test was passed, or `"Made {count} tests green"` when multiple tests were passed.
    - `XpEarned`: sum of `XpEarned` across all recent `PassedTest` events (e.g., `100` for 10 passed tests).
    - `CreatedAt`: the latest `CreatedAt` timestamp among the aggregated `PassedTest` events.
  - All `GamificationEventType.HallOfFameOvertake` events in the window are summarized or listed clearly (e.g., `"Overtake bonus (surpassed {count} student(s) in Hall of Fame)"`), and `GamificationEventType.HallOfFameTierUp` events are listed as `"Hall of Fame level up bonus ({TierName})"`.
  - All other non-`PassedTest` bonus events (`AssignmentCompleted`, `AssignmentCompletedQuickly`, `ChapterCompleted`, `TeamSynergy`) remain individual `RecentXpEventDto` items.
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
- **DR-7 (Hall of Fame League Tiers, Icons & Tie-Breaking)**:
  - The Hall of Fame orders opted-in students (excluding Lectors) by:
    1. `TotalXp` descending
    2. Timestamp of reaching `TotalXp` (`Events.Where(e => e.XpEarned > 0).Max(e => e.CreatedAt)`) ascending (student who reached that XP level first ranks higher; if `TotalXp == 0`, ordered by `Id` ascending).
  - The 8 Hall of Fame League Tiers (`HallOfFameTier`, ordered from lowest `1` to highest `8`) are defined by 1-based rank intervals:
    | Tier Enum (`int`) | Tier Name | Rank Interval | Icon |
    | :--- | :--- | :--- | :--- |
    | `Obsidian` (`8`) | **Obsidian** | Rank `1 – 15` | `🌋` (Volcanic Obsidian Crystal) |
    | `Diamond` (`7`) | **Diamond** | Rank `16 – 35` | `💎` (Radiant Diamond) |
    | `Gold` (`6`) | **Gold** | Rank `36 – 55` | `👑` (Golden Crown) |
    | `Silver` (`5`) | **Silver** | Rank `56 – 75` | `⚔️` (Silver Blades) |
    | `Bronze` (`4`) | **Bronze** | Rank `76 – 100` | `🛡️` (Bronze Shield) |
    | `Iron` (`3`) | **Iron** | Rank `101 – 150` | `⚒️` (Iron Anvil & Hammers) |
    | `Wood` (`2`) | **Wood** | Rank `151 – 200` | `🪵` (Carved Timber) |
    | `Paper` (`1`) | **Paper** | Rank `201+` | `📜` (Ancient Scroll) |
  - **Tier-Relative Display Rank Invariant**: Within each league tier, student ranking in `LeaderboardEntryDto.Rank`, `CurrentUserRank`, and `HallOfFameStudentOptionDto.Rank` **always starts at 1** (e.g., student at global rank 16 is `#1` in Diamond). Global ranks across the whole course are never exposed in the UI.
- **DR-8 (Overtake & Tier-Up Anti-Farming Invariant)**:
  - Each `StudentCourseProfile` tracks `BestHallOfFameRank` (`int?`) and `BestHallOfFameTier` (`HallOfFameTier?`).
  - Surpassing a student in the Hall of Fame ranking awards `+4 XP` per newly surpassed rank position (`previousEffectiveBestRank - newRank`, provided the student actually overtook at least one student above them and improved upon `BestHallOfFameRank`).
  - Rising a Hall of Fame tier awards `+5 XP` per newly reached tier above `BestHallOfFameTier` (`(int)newTier - (int)previousEffectiveBestTier`).
  - Because `BestHallOfFameRank` and `BestHallOfFameTier` only move upward (toward Rank 1 / `Obsidian`), two students trading places back and forth cannot farm infinite XP, and overtake/tier bonuses do not trigger cascading re-evaluations.
- **DR-9 (Teacher Exclusion from Gamification)**:
  - Users in the `Lector` role are never enrolled in `StudentCourseProfile` via test runs and are filtered out of all leaderboard queries (`GetForLeaderboardByCourseAndPeriodAsync`, `GetByCourseAndPeriodAsync`, Weekly Sprint badge evaluation, and assignment completion counts).

---

## 5. Authorization & Security Rules

- **All Gamification Endpoints (`/api/courses/{courseId}/gamification/...`)**: Require authentication (`[Authorize]`).
- **Students**:
  - Can view their own profile (`GET profile`) and enroll/update their own preferences (`POST enroll`).
  - Can view leaderboards (`GET leaderboard/weekly`, `GET leaderboard/hall-of-fame`, `GET leaderboard/classes`), course config (`GET config`), class groups (`GET classes`), and badge metadata (`GET badges`).
  - On `GET leaderboard/hall-of-fame`, students only receive the entries for their own current Hall of Fame tier (plus `NextTargetStudent` summary and the 4-tier progression header); any `perspectiveUserId` or `tier` query parameters passed by a non-Lector are ignored.
- **Lectors (`[Authorize(Roles = Role.Constants.Lector)]`)**:
  - Excluded from having a personal Hero Summary card and from appearing in any leaderboard rankings.
  - Can view **all** Hall of Fame tiers (either all entries across all tiers or filtered by a selected tier) OR pass `perspectiveUserId` on `GET leaderboard/hall-of-fame` and `GET leaderboard/weekly` to view the leaderboard from a specific student's perspective.
  - Exclusive permission to update course gamification configuration (`PUT config`).
  - Exclusive permission to create (`POST classes`) and delete (`DELETE classes/{classGroupId}`) course class groups.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Duplicate or empty `CourseClassGroup` name | Throws `ContractException` | `400 Bad Request` |
| Deleting a `CourseClassGroup` belonging to a different `courseId` | Throws `DataNotFoundException` | `404 Not Found` |
| Two students have identical `TotalXp` on Hall of Fame | Student with the earlier `Max(CreatedAt)` among positive-XP events is ranked higher | `200 OK` |
| Student is global Rank #1 (Apex Champion in Obsidian League) | `NextTargetStudent` is `null`; UI displays congratulatory "Apex Champion! You are currently #1 in the Obsidian League!" message instead of an overtake prompt | `200 OK` |
| Student is opted out of leaderboard (`ShowInLeaderBoard == false`) | Student does not appear in Hall of Fame rankings and does not earn overtake/tier-up bonuses while opted out | `200 OK` |
| Teacher submits passing test run from IDE | Ignored by `GamificationService.ProcessTestRunAsync`; no `StudentCourseProfile` created or updated | `200 OK` |
| Student passes `perspectiveUserId` of another student to `GET leaderboard/hall-of-fame` | Parameter is ignored for non-Lectors; endpoint always scopes to the authenticated student's own `userId` | `200 OK` |
| Rapidly refreshing profile within 10 minutes | `RecordLeaderboardVisit` returns `false`; `LastLeaderboardVisitDate` is not overwritten so recent XP banner stays visible for 10 minutes | `200 OK` |
| Exception thrown during `ProcessTestRunAsync` | Logged via `ILogger<GamificationService>`; `TestRun` registration still succeeds | `200 OK` |

---

## 7. Out of Scope

- Cross-course global leaderboards (all profiles, XP, and badges are scoped to `(UserId, CourseId, PeriodId)`).
- Manual XP adjustments by lectors.

---

## 8. Open Questions

- None.
