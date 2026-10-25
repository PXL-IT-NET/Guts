# GUTS Gamification User Stories

## Overview
The gamification system is designed to motivate students to achieve as many green tests as possible through consistent practice, healthy competition, and team synergy. It adapts key mechanics from Duolingo (Weekly Streaks, XP Progression, Weekly Sprints, Hall of Fame, Class Groups, and Badges) to automated unit testing in programming education.

---

## User Stories

### Story 1: Teacher Configures Course Classes & Gamification Features
**As a** Lector,  
**I want to** configure class groups (e.g. `1TIN-A`, `1TIN-B`) and toggle gamification features for my course,  
**So that** students can compete by class and gamification can be tailored to the course structure.

#### Acceptance Criteria
1. Lectors can create, edit, list, and delete class groups for a specific Course.
2. Lectors can configure gamification settings for a Course:
   - `IsGamificationEnabled` (default: `true`)
   - `IsWeeklyStreakEnabled` (default: `true`)
   - `IsLeaderboardEnabled` (default: `true`)
   - `IsClassCompetitionEnabled` (default: `true`)
   - `IsTeamSynergyEnabled` (default: `true`)
   - `IsBadgesEnabled` (default: `true`)
3. When gamification is disabled for a course, gamification navigation, statistics, and leaderboard widgets are hidden for students in that course.
4. All test runs associated with an Exam are strictly excluded from gamification (no XP, no streaks, no badges).

---

### Story 2: Student Opt-in & Avatar Selection
**As a** Student,  
**I want to** opt in to course gamification and customize my profile (avatar, class, real name vs pseudonym),  
**So that** I can participate in leaderboards while retaining control over my privacy.

#### Acceptance Criteria
1. Participation in leaderboards and class competitions is opt-in per student per course and academic period.
2. When opting in (or updating settings), the student selects:
   - **Display Mode**: Real Name vs Pseudonym. If Pseudonym is selected, the student provides a custom alias.
   - **Avatar**: Selected from a curated icon set (e.g., `CodeCat`, `DebugOwl`, `BitBot`, `SyntaxFox`, `Rocket`, `Wizard`, `JavaBeast`, `SharpCoder`).
   - **Class Group**: Chosen from the active class list configured for that course.
3. Students can update their avatar, pseudonym, and class group, or opt out at any time.
4. If opted out, the student is not displayed on public leaderboards, and their XP is excluded from class group aggregates.

---

### Story 3: Earning XP on Unique Green Tests & Assignments
**As a** Student,  
**I want to** earn XP when I pass tests,  
**So that** I receive immediate positive reinforcement for making progress.

#### Acceptance Criteria
1. **XP is awarded only for unique new passing tests**:
   - Re-running an already passing test yields 0 XP.
   - Passing a test for the first time on an assignment awards `+10 XP`.
2. **Assignment Completion Bonus**:
   - When all tests for an assignment are passing for the first time, award a bonus of `+50 XP`.
3. **Chapter Milestone Bonus**:
   - When all assignments in a chapter are 100% completed, award a milestone bonus of `+100 XP`.
4. XP contributes to a student Level:
   - Level formula: `Level = floor(sqrt(XP / 25)) + 1` (or corresponding milestone XP brackets).
   - Each level displays a level title (e.g., *Byte Novice*, *Code Apprentice*, *Syntax Sorcerer*, *Algorithm Ace*, *Code Titan*).

---

### Story 4: Weekly Coding Streaks 🔥
**As a** Student,  
**I want to** build and maintain a weekly coding streak,  
**So that** I am encouraged to practice regularly every week instead of cramming right before deadlines.

#### Acceptance Criteria
1. A streak increases by +1 when a student records at least one unique new passing test during a calendar week (ISO 8601 week: Monday through Sunday).
2. If a calendar week concludes without any passing tests in the course, the streak resets to 0.
3. The student dashboard displays the current streak count, highest streak count, and a flame indicator showing whether this week's requirement has already been met.

---

### Story 5: Weekly Sprint Leaderboard & Course Hall of Fame 🏆
**As a** Student,  
**I want to** view the Weekly Sprint and All-Time Hall of Fame leaderboards,  
**So that** I can see how I compare to my peers and celebrate top achievements.

#### Acceptance Criteria
1. **Weekly Sprint Leaderboard**:
   - Ranks opted-in students based on XP earned within the current calendar week.
   - Resets automatically at the start of each week (Monday 00:00 UTC).
2. **Course Hall of Fame**:
   - Ranks opted-in students across the course and current academic period by total XP.
   - Displays student rank, avatar, display name (pseudonym or real name), class group, level, total green tests, and current weekly streak.
3. The requesting student's row is highlighted, and their current standing is always visible even if they are outside the top 10.

---

### Story 6: Inter-Class Competition & Project Team Synergy 🤝
**As a** Student,  
**I want to** contribute to my class standing and collaborate with my project team,  
**So that** we can compete together as a group and foster team spirit.

#### Acceptance Criteria
1. **Class Group Leaderboard**:
   - Ranks class groups within the course by **Average XP per opted-in student** (preventing class size disparity from skewing the results).
   - Shows total participating students, total green tests, and average XP.
2. **Project Team Synergy**:
   - When all members of a project team have passing tests on a project component, each team member receives a `+30 XP` Team Synergy bonus.

---

### Story 7: Badges & Achievements Catalog 🎖️
**As a** Student,  
**I want to** unlock achievement badges for special coding accomplishments,  
**So that** I have long-term goals and fun milestones to collect.

#### Acceptance Criteria
1. Badges are evaluated and unlocked automatically upon processing test runs.
2. The initial badge catalog includes:
   - 🌱 **First Spark**: Pass your very first test in the course.
   - ⚡ **Speed Demon**: Complete all tests of an assignment within 24 hours of your first run.
   - 🧹 **Clean Sweep**: Solve 100% of the tests across an entire chapter.
   - 🦉 **Night Owl**: Submit a passing test between 00:00 and 05:00.
   - 🌅 **Early Bird**: Submit a passing test between 05:00 and 08:00.
   - ⚔️ **Weekend Warrior**: Pass a new test on a Saturday or Sunday.
   - 💯 **Century Club**: Reach 100 unique green tests in the course.
   - 🔥 **On Fire**: Maintain a 4-week coding streak.
   - 🛡️ **Iron Will**: Maintain an 8-week coding streak.
   - 🥇 **Sprint Gold**: Finish #1 on the weekly sprint leaderboard.
   - 🥈 **Sprint Silver**: Finish in the top 2 of the weekly sprint leaderboard.
   - 🥉 **Sprint Bronze**: Finish in the top 3 of the weekly sprint leaderboard.
   - 🎖️ **Sprint Top 4**: Finish in the top 4 of the weekly sprint leaderboard.
   - ⭐ **Sprint Top 5**: Finish in the top 5 of the weekly sprint leaderboard.
   - 🤝 **Team Powerhouse**: Complete a project component where every team member has passing tests.
3. Unlocked badges appear on the student's gamification profile and badge showcase with unlock timestamps.

