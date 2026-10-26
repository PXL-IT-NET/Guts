# Specification: Project Team Peer Assessments

- **Feature ID**: `001-peer-assessments`
- **Status**: `Implemented` (Baseline Reference Spec)
- **Created**: `2026-10-27`

---

## 1. Context & Problem Statement

In project-based courses (`Project` topics), students work together in teams (`ProjectTeamAggregate`). Because individual effort, technical contribution, and cooperation can vary significantly within a team, lectors need a structured, transparent way for team members to evaluate themselves and their peers at specific milestones (`ProjectAssessment`).

The **Project Team Peer Assessments** feature enables:
1. **Lectors** to configure assessment windows (`OpenOnUtc` to `DeadlineUtc`) for a project and inspect detailed individual/team peer evaluation breakdowns.
2. **Students** to evaluate themselves and every teammate across three dimensions (**Cooperation**, **Contribution**, and **Effort**) on a standardized 0–5 relative scale (`AssessmentScore`), and to view their own aggregated results versus the team average once evaluations are submitted.

---

## 2. User Stories & Actors

- **Actors**: `Student`, `Lector (Teacher)`

- **US-1 (Configure Project Assessment)**: As a **Lector**, I want to create, update, or delete a `ProjectAssessment` with a description, opening timestamp, and deadline for a project so that teams can evaluate each other during a defined window.
- **US-2 (Evaluate Self & Peers)**: As a **Student**, I want to submit scores for **Cooperation**, **Contribution**, and **Effort** (plus a mandatory explanation when deviating from average) for myself and every member of my `ProjectTeam` so that our individual contributions are fairly recorded.
- **US-3 (Track Team Completion Status)**: As a **Student** or **Lector**, I want to see whether a team's assessment is complete and which team members still need to submit evaluations.
- **US-4 (View Student Summary Result)**: As a **Student**, I want to view my own aggregated assessment result (comparing my self-assessment against my peers' average scores and the team averages, without revealing individual peer identities/explanations) so I can reflect on my performance.
- **US-5 (Inspect Detailed Lector Results)**: As a **Lector**, I want to view the complete assessment results for a project team—including every individual peer evaluation and written explanation—so I can grade or moderate team members fairly.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Managing Project Assessments (Lector)
- **Given** an existing project (`ProjectId > 0`)
- **When** a Lector creates or updates a `ProjectAssessment` with a non-empty `Description`, `OpenOnUtc`, and `DeadlineUtc`
- **Then** the `ProjectAssessment` is persisted if `OpenOnUtc` and `DeadlineUtc` are in UTC, `OpenOnUtc < DeadlineUtc`, and (on creation) `DeadlineUtc > DateTime.UtcNow`.

### AC-2: Lazy Provisioning of a Team Assessment
- **Given** a `ProjectAssessment` that has already opened (`OpenOnUtc <= DateTime.UtcNow`) and a `ProjectTeam` with at least one member
- **When** the status, peer evaluations, or results are requested for `(projectAssessmentId, teamId)` and no `ProjectTeamAssessment` exists yet
- **Then** a new `ProjectTeamAssessment` is automatically created and persisted for that assessment and team.

### AC-3: Retrieving Stored and Missing Peer Assessments for a Student
- **Given** a student who is a member of the `ProjectTeam`
- **When** the student requests their peer assessments for a `ProjectTeamAssessment`
- **Then** the system returns all stored `PeerAssessment` entries authored by that student plus newly initialized placeholder `PeerAssessment` entries (`NullScore = -1`) for any team members (including self) not yet evaluated.

### AC-4: Submitting and Validating Peer Assessments
- **Given** a student who is a member of the `ProjectTeam` and a `ProjectTeamAssessment` where `IsComplete == false`
- **When** the student saves their peer assessments for the team
- **Then** each evaluation is added or updated, provided that:
  1. All three scores (`CooperationScore`, `ContributionScore`, `EffortScore`) are valid and not `NullScore (-1)`.
  2. An `Explanation` is non-empty whenever any of the three scores is not `Average (3)`.
  3. Once the student has evaluated all $N$ team members, not all members are rated strictly above `Average (3)` on the same criterion, and not all members are rated strictly below `Average (3)` on the same criterion.

### AC-5: Locking Completed Team Assessments
- **Given** a `ProjectTeam` with $N$ members where all $N \times N$ peer evaluations have been saved (`IsComplete == true`)
- **When** any team member attempts to add or update a peer assessment
- **Then** the operation is rejected with a `ContractException`.

### AC-6: Calculating Individual & Team Assessment Results
- **Given** a `ProjectTeamAssessment` where the subject has completed their self-assessment and at least one peer has evaluated the subject
- **When** an `AssessmentResult` is generated for the subject
- **Then** `AverageResult`, `EffortResult`, `ContributionResult`, and `CooperationResult` are computed with:
  - `SelfValue` / `SelfScore` (the subject's self-evaluation)
  - `PeerValue` / `PeerScore` (mean of evaluations of the subject by other teammates)
  - `Value` / `Score` (mean of all evaluations of the subject, including self)
  - `AverageValue`, `AverageSelfValue`, and `AveragePeerValue` across the entire team
- **And** individual `PeerAssessments` (with evaluator identity and explanation) are included **only** for Lectors (`includePeerAssessments = true`) and omitted for Students (`includePeerAssessments = false`).

---

## 4. Domain Rules & Invariants

- **DR-1 (`AssessmentScore` Scale)**: Scores are represented by the `AssessmentScore` value object with values:
  - `-1` (`NullScore` — ungraded)
  - `0` (`NoAddedValue`)
  - `1` (`WayBelowAverage`)
  - `2` (`BelowAverage`)
  - `3` (`Average`)
  - `4` (`AboveAverage`)
  - `5` (`WayAboveAverage`)
  - Values $> 5$ throw `ContractException`; values $< 0$ normalize to `-1`. Double-to-score conversion uses `MidpointRounding.AwayFromZero`.
- **DR-2 (`ProjectAssessment` Dates)**: `Description` cannot be empty, `OpenOnUtc` and `DeadlineUtc` must have `DateTimeKind.Utc`, `OpenOnUtc < DeadlineUtc`, and on creation `DeadlineUtc > DateTime.UtcNow`.
- **DR-3 (`ProjectTeamAssessment` Creation)**: Requires a stored `ProjectAssessment` (`Id > 0`) that is currently open (`OpenOnUtc <= DateTime.UtcNow`) and a non-empty `ProjectTeam` (`TeamUsers.Count > 0`) with loaded `User` entities.
- **DR-4 (Team Membership Check)**: Only members of `Team.TeamUsers` can author (`userId`) or be the target (`subjectId`) of a `PeerAssessment` within that team.
- **DR-5 (Immutability After Completion)**: `IsComplete` is true when `PeerAssessments.Count == Math.Pow(Team.TeamUsers.Count, 2)`. Once complete, no peer assessment in that `ProjectTeamAssessment` can be modified.
- **DR-6 (Mandatory Explanation for Non-Average Scores)**: If `CooperationScore != Average (3)` OR `ContributionScore != Average (3)` OR `EffortScore != Average (3)`, `Explanation` must not be null or whitespace.
- **DR-7 (Zero-Sum / Relative Average Guard)**: When a user has evaluated all team members, they cannot rate **every** member $> 3$ (`Average`) on Contribution, Cooperation, or Effort, nor can they rate **every** member $< 3$ (`Average`) on Contribution, Cooperation, or Effort.
- **DR-8 (Result Calculation Preconditions)**: Creating an `AssessmentResult` for a subject requires both a self-assessment by the subject and at least one peer assessment from another team member.

---

## 5. Authorization & Security Rules

- **Students**:
  - Can view `ProjectAssessment` metadata (`GET /api/project-assessments/...`).
  - Can view team assessment completion status (`GET .../status`).
  - Can view and save **only their own** authored peer assessments (`dto.UserId == authenticatedUserId`).
  - Can view **only their own** aggregated summary result (`GET .../my-result`), which excludes individual peer evaluations (`PeerAssessments` collection is empty).
- **Lectors (`ApiConstants.LectorsOnlyPolicy`)**:
  - Exclusive permission to create (`POST`), update (`PUT`), and delete (`DELETE`) `ProjectAssessment` definitions.
  - Exclusive permission to view detailed team results (`GET .../detailed-results`), which include raw individual peer evaluations and explanations.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Creating team assessment before `OpenOnUtc` | Throws `ContractException` | `400 Bad Request` |
| Non-Average score submitted without explanation | Throws `ContractException` | `400 Bad Request` |
| Student rates all teammates above (or all below) Average | Throws `ContractException` in `ValidateAssessmentsOf` | `400 Bad Request` |
| Student attempts to save assessments for another `userId` | Throws `ContractException` | `400 Bad Request` |
| Modifying assessments after `IsComplete == true` | Throws `ContractException` | `400 Bad Request` |
| Requesting results before self or peers have evaluated | Throws `ContractException` | `400 Bad Request` |
| Unknown `projectAssessmentId` or `teamId` when creating team assessment | `DataNotFoundException` caught and rethrown as `ContractException` | `400 Bad Request` |

---

## 7. Out of Scope

- Direct automatic synchronization of peer assessment scores into `ExamAggregate` grades (handled separately via exam/score administration).
- Email/notification reminders for pending peer assessments.

---

## 8. Open Questions

- None (Baseline specification of existing production feature).

