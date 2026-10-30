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

Teachers (`Lector`) are completely excluded from all rankings and do not have a personal Hero Summary card. Instead, teachers can view all Hall of Fame levels (teacher overview) or select a specific student to inspect that student's Hero Summary card, earned badges, and the leaderboards from that student's exact perspective.

---

## 2. User Stories & Actors

- **Actors**: `Student`, `Lector (Teacher)`, `Background Scheduler (SprintLeaderboardBackgroundService, StreakReminderBackgroundService)`

- **US-1 (Configure Course Gamification & Class Groups)**: As a **Lector**, I want to toggle gamification and its individual sub-features (Weekly Streaks, Leaderboards, Class Competition, Team Synergy, Badges) per course, and manage `CourseClassGroup` entities so I can tailor the experience to my course structure.
- **US-2 (Manage Student Profile & Privacy)**: As a **Student**, I want to customize my avatar, pseudonym, class group membership, and leaderboard visibility (`ShowInLeaderBoard`, `UsePseudonym`) so I can participate comfortably.
- **US-3 (Earn XP, Levels, Streaks, Badges, and Ranking Bonuses from Test Runs)**: As a **Student**, I want to automatically earn XP, level progression, weekly streak increments, badges, and Hall of Fame overtake (`+4 XP`) and tier-promotion (`+5 XP`) bonuses when my IDE submits passing test runs (`TestRun`) that yield new XP for exercises and project components.
- **US-4 (Review Recent XP Gains)**: As a **Student**, I want to see a concise breakdown of XP events earned since my last visit to the gamification dashboard (`RecentXpGainDto`), where all passing test events are summarized into a single row (`"Made {count} test(s) green"`) while bonus events (including overtake and tier-up bonuses) are listed individually.
- **US-5 (Compete on Tiered Hall of Fame, View Leaderboards & Teacher Inspection of Student Perspective)**:
  - As a **Student**, I want to view the **Weekly Sprint Leaderboard**, the **Class Championship Leaderboard**, and **only my current league tier** of the **Hall of Fame Leaderboard** where rankings always start at #1 (with gold/silver/bronze medals for the top 3 in my tier and no demotivating global rank displayed), alongside a 4-icon tier progression header (1 previous, current enlarged, 2 next) and a motivational callout to overtake the student directly above me.
  - As a **Lector (Teacher)**, I want to be excluded from all rankings and not have a personal Hero Summary card; instead, I want to browse **all Hall of Fame levels** (teacher overview) with tier-relative ranks (no global ranks) AND **select a student** to inspect the **Hero Summary card** and **earned badges** of that student and see the **leaderboards** (Weekly Sprint and Hall of Fame tier) from that student's perspective (in read-only mode).
- **US-6 (Automated Weekly Sprint Badge Awarding)**: As the **Background Scheduler**, I want to evaluate the previous ISO week's Sprint Leaderboard every Monday at `00:01 UTC` (using a Friday `23:59:59 UTC` cutoff) and award top-5 sprint badges (`SprintGold` through `SprintTop5`) to eligible students.
- **US-7 (Weekly Streak Loss & All-Tests-Passed Protection)**: As a **Student**, I want my weekly coding streak (`CurrentStreak`) to be lost if I earn no XP in the last ISO week, **except** when I have already passed all currently available tests in the course, in which case my streak is kept so I am not penalized for having no remaining tests to solve.
- **US-8 (Thursday Midday Streak Reminder Notification & Throttled Queue)**:
  - As a **Student** with an active streak (`CurrentStreak > 0`), I want to receive an encouraging and supportive reminder email every Thursday around midday if I haven't earned any XP yet in the current week, highlighting my current streak, my next Hall of Fame target to beat, and smart assignment suggestions (focusing on exercises I've started but not yet completed), so that I am motivated to practice and keep my streak alive before the week ends.
  - As the **Background Scheduler**, I want to periodically check courses with gamification and streaks enabled every Thursday around midday, queue personalized reminder emails for eligible students who still have incomplete tests in the course, and dispatch them through `IMailSender` using a throttled queue to prevent SMTP servers from classifying the emails as spam.

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
      - Teachers do **not** have a personal Hero Summary card.
      - At the top of the Gamification page, teachers see a **"Student Perspective" selector** allowing them to choose between:
        1. **View All Levels (Default Teacher Mode / No Student Selected)**:
           - Browse and switch between all 8 Hall of Fame tiers (or view all ranked students across all tiers), with each tier displaying tier-relative ranks (starting at 1) without global ranks.
           - Personal Hero Summary card is hidden.
           - The Badges tab displays the general badge catalogue without earned highlights.
        2. **Select a Specific Student**:
           - **Hero Summary Card**: Displays the selected student's Hero Summary card (`app-course-gamification-hero`) showing their level, level title, total XP, streak progress, green tests count, avatar, and pseudonym/name in **read-only mode** (profile customization / settings button is hidden).
           - **Badges**: The Badges tab (`app-course-gamification-badges`) displays that student's earned badges with their unlock dates, and the tab label indicates their earned count (`Badges ({count})`).
           - **Leaderboards**:
             - **Hall of Fame**: Displays only the league tier slice of the selected student with tier-relative ranks (starting at 1), the 4-icon progression header centered on their tier, and the overtake target callout relative to them.
             - **Weekly Sprint**: Highlights the selected student (`IsCurrentUser == true`) and reflects their weekly sprint status (or callout banner if not on the board).
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
- **And** when a profile is requested with `perspectiveUserId` (`GET /api/courses/{courseId}/gamification/profile?perspectiveUserId={studentUserId}`):
  - If requested by a teacher (`Lector` role): returns the specified student's `StudentGamificationProfileDto` (including `TotalXp`, `Level`, `LevelTitle`, `CurrentWeeklyStreak`, `TotalGreenTests`, and `EarnedBadges`) for read-only inspection, and does **not** update `LastLeaderboardVisitDate` (preserving the student's recent gains window).
  - If requested by a student (`Student` role): `perspectiveUserId` is ignored and the authenticated student's own profile is returned.

### AC-6: Weekly Streak Loss & All-Tests-Passed Protection
- **Given** a student with an active `WeeklyStreak` (`CurrentStreak > 0`) in a course where `IsGamificationEnabled == true` and `IsWeeklyStreakEnabled == true`
- **When** the previous ISO week has elapsed and the student earned `0 XP` during that week:
  - **If** the student has **not** yet passed all available tests in the course (across all topics/assignments in the active period, or when no tests exist yet), **Then** the student loses their weekly streak (`CurrentStreak` is reset to `0`, while `BestStreak` is preserved).
  - **If** the student has already passed **all** available tests in the course (and the course has at least 1 test), **Then** the student's `CurrentStreak` is kept (preserved without resetting) so they can continue their streak once new tests are provisioned.

### AC-7: Thursday Streak Reminder Notifications & Throttled Queue
- **Given** active courses in the current academic period where `IsGamificationEnabled == true` and `IsWeeklyStreakEnabled == true`
- **When** the scheduled execution occurs each **Thursday around midday** (`12:00 UTC`):
  - **Eligibility Filter**: Evaluates each student profile (`StudentCourseProfile`) in the course. A student is eligible to receive a streak reminder email **if and only if**:
    1. The user has the `Student` role (lectors are strictly excluded).
    2. The student has an active streak (`CurrentStreak > 0`).
    3. The student has earned `0 XP` in the current ISO week (from Monday `00:00:00 UTC` up to the execution time).
    4. The student has **not** completed 100% of all currently available tests in the course (i.e. course has tests and student has at least one unpassed test remaining; students who have passed all tests are protected under `AC-6` and do not receive reminders).
- **Then** for each eligible student, a personalized reminder email is prepared and enqueued:
  - **Subject**: `Keep your momentum going in {CourseName}!` (Encouraging and supportive).
  - **Greeting**: Personalized with student's first name (`user.FirstName`), falling back to `Pseudonym` (or `"Student"` if both are empty).
  - **Streak Highlight**: Reminds the student of their current streak (e.g., `"You're currently on a {CurrentStreak}-week coding streak! Don't lose your momentum — earn XP before Sunday 23:59 UTC to keep it going."`).
  - **Next Target to Beat**: If the student is opted into the leaderboard (`ShowInLeaderBoard == true`) and has a target student immediately above them in the course Hall of Fame (`NextTargetStudent != null`), highlights that student (e.g., `"You're close to overtaking {TargetStudentName} in the Hall of Fame! Passing a few more tests could boost your standing."`). If the student is #1 (Apex Champion) or opted out, this section is omitted.
  - **Smart Assignment Recommendations**: Recommends up to 2 specific assignments to help the student earn XP quickly:
    1. **In-Progress Assignments**: Assignments where the student has passed at least 1 test but has remaining unpassed tests, ordered by assignment sequence.
    2. **Untouched Fallbacks**: If fewer than 2 in-progress assignments exist, recommends the earliest unattempted assignment(s) in the course (0 passing tests).
    3. Each recommended assignment includes its title and a direct link to the assignment in the GUTS web application.
  - **Language**: English (matching existing GUTS email templates).
- **And** the emails are dispatched using an asynchronous background queue via `IMailSender`:
  - **Throttling / Anti-Spam Rate Limiting**: The queue dispatches emails sequentially with an enforced throttle delay (e.g. 1 second between consecutive emails) to ensure the SMTP server does not flag or throttle the messages as bulk spam.
  - **Resilience**: An exception when sending an email to a single recipient is logged via `ILogger` and does not terminate the background processing for the remaining queue items.

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
- **DR-10 (Streak Reminder Eligibility & All-Tests Protection Alignment)**:
  - A student is eligible for a Thursday streak reminder in a course if and only if:
    1. `User` is in the `Student` role (lectors are excluded).
    2. `WeeklyStreak.CurrentStreak > 0`.
    3. The student has earned `0 XP` in the course during the current ISO week.
    4. Total passed tests for the student in the course is strictly less than the total available tests in the course. (If the student has already passed 100% of tests, their streak is protected per `DR-3` and no reminder email is sent).
- **DR-11 (Smart Assignment Recommendation Strategy)**:
  - Up to 2 assignments are selected to motivate the student:
    1. Primary candidates: Assignments in which the student has passed $\ge 1$ test but $< 100\%$ of tests (in-progress), ordered by topic/assignment sequence.
    2. Secondary candidates: If fewer than 2 in-progress assignments exist, the earliest unattempted assignments (0 tests passed) in the course, ordered by sequence.
    3. Fully completed assignments are excluded.
- **DR-12 (SMTP Throttled Delivery Invariant)**:
  - Bulk email notifications generated by the reminder service must be dispatched through an asynchronous queue with an enforced sequential rate limit (e.g. 1 second per message) to prevent overwhelming the SMTP server and avoid spam categorization.

---

## 5. Authorization & Security Rules

- **All Gamification Endpoints (`/api/courses/{courseId}/gamification/...`)**: Require authentication (`[Authorize]`).
- **Students**:
  - Can view their own profile (`GET profile`) and enroll/update their own preferences (`POST enroll`).
  - Can view leaderboards (`GET leaderboard/weekly`, `GET leaderboard/hall-of-fame`, `GET leaderboard/classes`), course config (`GET config`), class groups (`GET classes`), and badge metadata (`GET badges`).
  - On `GET leaderboard/hall-of-fame`, students only receive the entries for their own current Hall of Fame tier (plus `NextTargetStudent` summary and the 4-tier progression header); any `perspectiveUserId` or `tier` query parameters passed by a non-Lector are ignored.
- **Lectors (`[Authorize(Roles = Role.Constants.Lector)]`)**:
  - Excluded from having a personal Hero Summary card and from appearing in any leaderboard rankings.
  - Can view **all** Hall of Fame tiers (either all entries across all tiers or filtered by a selected tier).
  - Can pass `perspectiveUserId` on `GET profile`, `GET leaderboard/hall-of-fame`, and `GET leaderboard/weekly` to inspect that student's Hero Summary card, earned badges, and leaderboard perspective in read-only mode.
  - Cannot modify a student's profile or privacy preferences (profile customization is disabled/hidden in student perspective mode).
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
| Teacher inspects student profile via `GET profile?perspectiveUserId=X` | Profile is returned without updating `LastLeaderboardVisitDate` so student's recent gains notification is preserved | `200 OK` |
| Student passes `perspectiveUserId` of another student to `GET leaderboard/hall-of-fame` or `GET profile` | Parameter is ignored for non-Lectors; endpoint always scopes to the authenticated student's own `userId` | `200 OK` |
| Teacher selects student in perspective dropdown | Hero Summary card and Badges tab display the selected student's data in read-only mode (settings modal hidden) | `200 OK` |
| Rapidly refreshing profile within 10 minutes | `RecordLeaderboardVisit` returns `false`; `LastLeaderboardVisitDate` is not overwritten so recent XP banner stays visible for 10 minutes | `200 OK` |
| Exception thrown during `ProcessTestRunAsync` | Logged via `ILogger<GamificationService>`; `TestRun` registration still succeeds | `200 OK` |
| Student has passed 100% of available tests in course | Streak is protected; no Thursday streak reminder email is sent | N/A (Background) |
| Student is #1 in Hall of Fame (Apex Champion) or opted out | Email is sent without the overtake target section, focusing on streak status and recommended exercises | N/A (Background) |
| Student user record has no first name | Salutation in email greeting falls back to `Pseudonym` or `"Student"` | N/A (Background) |
| SMTP failure sending email to a single recipient | Error logged via `ILogger`; background worker continues processing remaining students in queue | N/A (Background) |
| Student has streaks at risk in multiple courses | Queues a distinct personalized reminder email for each eligible course | N/A (Background) |

---

## 7. Out of Scope

- Cross-course global leaderboards (all profiles, XP, and badges are scoped to `(UserId, CourseId, PeriodId)`).
- Manual XP adjustments by lectors.

---

## 8. Open Questions

- None.
