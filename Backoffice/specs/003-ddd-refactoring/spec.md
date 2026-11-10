# Specification: DDD Domain Deep Modeling & Deprecated Exam Removal

- **Feature ID**: `003-ddd-refactoring`
- **Status**: `Implemented`
- **Created**: `2026-11-10`
- **Updated**: `2026-11-10`

---

## 1. Context & Problem Statement

Over time, several core parts of the GUTS Backoffice experienced domain logic leakage, where domain invariants, aggregations, score calculations, and default value formatting were implemented procedurally within Application Services (`Guts.Application`) or API Converters (`Guts.Api`). Entities in `Guts.Domain` were frequently anemic, exposing mutable public setters and unencapsulated collections (`ICollection<T>`).

Furthermore, the `Exam` aggregate (`Exams`, `ExamParts`, and `AssignmentEvaluation`) represented legacy, deprecated functionality that added architectural baggage, unnecessary test overhead, and cognitive complexity.

To align GUTS Backoffice with Eric Evans' *Domain-Driven Design* (Part III: *Refactoring Toward Deeper Insight*), this specification documents:
1. Complete removal of the deprecated `Exam` aggregate and its dependencies across backend and frontend.
2. Encapsulation of domain logic, state invariants, and calculations inside rich Aggregates, Value Objects, Domain Policies, and pure Domain Services in `Guts.Domain`.
3. Sanitization of Application Services into thin use-case orchestrators and API Converters into pure DTO mappers.

---

## 2. User Stories & Actors

- **Actors**: `Lector (Teacher)`, `Student`, `Administrator`, `System Developer / Maintainer`

- **US-1 (Clean Architecture & Maintained Boundaries)**: As a **System Developer**, I want domain invariants, business rules, and multi-entity calculations strictly encapsulated within `Guts.Domain` so that Application and API layers cannot violate domain state invariants or bypass validation.
- **US-2 (Deprecated Functionality Purge)**: As a **System Maintainer**, I want all deprecated Exam tables, models, services, controllers, and Angular components removed so that the codebase is leaner, easier to navigate, and free of obsolete code paths.
- **US-3 (Intention-Revealing Domain Models)**: As a **System Developer**, I want aggregates and entities (`Assignment`, `Project`, `ProjectTeam`, `LoginSession`, `TestRun`, `User`) to expose clear, intention-revealing methods and properties so that calling code communicates in the Ubiquitous Language.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Deprecated Exam Removal & EF Core Migration
- **Given** the deprecated `Exam` subsystem
- **When** the aggregate is removed from the solution
- **Then**:
  - `Guts.Domain.ExamAggregate` (and all types: `Exam`, `ExamPart`, `AssignmentEvaluation`) is deleted.
  - `IExamService`, `ExamService`, `IExamRepository`, `ExamDbRepository`, `IExamPartRepository`, `ExamPartDbRepository` are deleted.
  - `ExamController` and all associated API request/response models are deleted.
  - Angular frontend components (`exam`, `exampart`), services (`exam.service.ts`), and viewmodels (`exam.model.ts`) are deleted.
  - EF Core migration `20261110194842_RemoveExamAggregate` drops tables `Exams`, `ExamParts`, and `AssignmentEvaluation`.
  - `dotnet ef migrations has-pending-model-changes` reports 0 pending changes.

### AC-2: Course Progress & Statistics Domain Calculation
- **Given** a `Course`, its `Chapter` collection with `Assignment`s and `Test`s, and the latest `TestResult`s
- **When** course progress is computed
- **Then**:
  - Calculation is executed by pure domain service `CourseProgressCalculator` (`ICourseProgressCalculator`) in `Guts.Domain.CourseAggregate`.
  - Produces an immutable `CourseProgressReport` containing `CourseStudentProgress`, `CourseChapterProgress`, and `CourseAssignmentProgress`.
  - Calculates per-student completed assignments, passed tests, and completion percentage using pure domain logic.
  - `CourseService.GetCourseStatisticsAsync` acts solely as an orchestrator loading entities and mapping the domain report to `CourseStatisticsDto`.

### AC-3: Assignment Passage Distribution & TestRun Summary Value Objects
- **Given** a set of `TestResult`s for an assignment
- **When** statistics and test run summaries are requested
- **Then**:
  - Value Object `AssignmentPassageDistribution` (`Guts.Domain.AssignmentAggregate`) groups test results by user/team, counts passed tests, and produces `TestPassageCount` intervals.
  - Value Object `TestRunSummary` (`Guts.Domain.TestRunAggregate`) computes `FirstRunDateTime`, `LastRunDateTime`, and `NumberOfRuns` from a list of `TestRun` entities.
  - Procedural LINQ in `AssignmentWithResultsConverter` and `AssignmentService` is replaced with calls to these Value Objects.

### AC-4: LoginSession Aggregate Encapsulation
- **Given** a `LoginSession` in `Guts.Domain.LoginSessionAggregate`
- **When** a session is created, cancelled, attached to a login token, or checked for expiration
- **Then**:
  - Properties have `internal set;` and are protected against unauthorized external mutation.
  - Factory method `LoginSession.Create(ipAddress)` enforces invariant that IP address cannot be empty.
  - Method `AttachLoginToken(publicToken, tokenText, now)` enforces that the session is not expired before attaching.
  - Method `Cancel(now)` marks session as cancelled.
  - Method `IsExpired(now, timeout)` determines validity based on session age and cancellation state.
  - `LoginSessionService` delegates session lifecycle directly to aggregate methods.

### AC-5: Assignment Recommendation Domain Policy
- **Given** a student's passed test IDs across a course's assignments
- **When** smart streak reminder recommendations are built
- **Then**:
  - Pure domain policy `AssignmentRecommendationPolicy.RecommendAssignments` selects candidates without any application-layer branching:
    1. Filters out completed assignments (100% tests passed).
    2. Prioritizes in-progress assignments (passed $\ge 1$ test but $< 100\%$) ordered by completion progress descending.
    3. Falls back to earliest unattempted assignments (0 tests passed).
    4. Limits output to `maxRecommendations` (default 2) returning `RecommendedAssignment`.
  - `StreakReminderService.BuildRecommendedAssignments` delegates directly to this domain policy.

### AC-6: Gamification Event Aggregation (`RecentGainsSummary`)
- **Given** recent `GamificationEvent` entities earned since a student's last visit
- **When** recent gains are displayed in the gamification dashboard
- **Then**:
  - Value Object `RecentGainsSummary` in `Guts.Domain.GamificationAggregate`:
    - Summarizes all `PassedTest` events into `"Made {count} test(s) green"` with total XP and latest timestamp.
    - Summarizes `HallOfFameOvertake` events into a single entry with total XP.
    - Maps `HallOfFameTierUp`, `HallOfFameRankDefended`, and other bonuses individually to `AggregatedXpGain`.
    - Computes `TotalXpGained`.
  - Replaces private procedural method `BuildRecentEventDtos` in `GamificationService`.

### AC-7: Project & ProjectTeam Encapsulation
- **Given** `Project` and `ProjectTeam` aggregates
- **When** teams are added, generated, or inspected for membership
- **Then**:
  - `Project.AddTeam(name)` enforces non-empty and non-duplicate team names.
  - `Project.GenerateTeams(baseName, from, to)` validates range and batch size ($\le 100$) and generates teams.
  - `Project.FindTeamOfUser(userId)` and `ProjectTeam.HasMember(userId)` encapsulate membership queries.
  - `ProjectTeam.Rename(newName)` enforces non-empty names.
  - `ProjectTeam.EnsureBelongsTo(courseId, projectCode)` validates project and course ownership.
  - `AssignmentController` uses `team.HasMember(userId)` instead of manual collection inspection.

### AC-8: Intention-Revealing Entity Properties
- **Given** domain entities `Assignment`, `TestRun`, and `User`
- **When** accessed across application and presentation layers
- **Then**:
  - `Assignment.Description`: Automatically formats positive integer codes (e.g., `"1"`) as `"Assignment 1"` when no custom description is set, eliminating procedural `EnsureAssignmentDescription` in `TopicConverter`.
  - `TestRun`: Exposes `HasPassingTests`, `PassedTestsCount`, and `FailedTestsCount`, simplifying condition checks in `GamificationService` and log formatting in `TestRunController`.
  - `User`: Exposes computed `FullName => $"{FirstName} {LastName}".Trim()`, eliminating repeated string interpolation across `UserConverter`, `Mapper`, and `AssignmentService`.

---

## 4. Domain Rules & Invariants

- **DR-1 (Aggregate Encapsulation)**: Domain entities must not expose public property setters or public mutable collections for state that requires invariant enforcement.
- **DR-2 (Thin Application Orchestrators)**: Application services must only orchestrate repository loading, domain method invocation, and persistence; they must not perform calculations, eligibility checks, or collection mutations.
- **DR-3 (Stateless Pure Domain Services & Policies)**: Multi-entity calculations (`CourseProgressCalculator`, `WeeklySprintCompetition`, `ClassGroupCompetition`, `AssignmentRecommendationPolicy`) must be pure domain services or static policies in `Guts.Domain` with zero database or I/O dependencies.
- **DR-4 (Precondition Enforcement)**: Preconditions on method arguments and invariants must be validated using `Contracts.Require(...)` from `Guts.Common`.

---

## 5. Authorization & Security Rules

- Unchanged from standard GUTS permissions:
  - Lectors manage course settings, topics, teams, and view all student data.
  - Students access only their own submissions, summaries, and assigned project teams.
  - Direct aggregate state changes cannot be made via API model binders.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Attempting to attach login token to expired session | Throws `ContractException` ("Cannot attach a login token to an expired session") | `400 Bad Request` |
| Attempting to add duplicate team name to project | Throws `ContractException` ("A team with the same name already exists") | `400 Bad Request` |
| Generating more than 100 teams in a batch | Throws `ContractException` ("Cannot generate more than 100 teams in a single request.") | `400 Bad Request` |
| Empty or whitespace test name / team name / code | Throws `ContractException` | `400 Bad Request` |

---

## 7. Out of Scope

- Modifying public REST API response contracts (all existing API response schemas remain backwards-compatible).
- Database schema changes beyond dropping the deprecated `Exam` aggregate tables.

