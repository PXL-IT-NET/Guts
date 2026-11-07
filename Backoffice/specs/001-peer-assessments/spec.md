# Specification: Project Team Peer Assessments

- **Feature ID**: `001-peer-assessments`
- **Status**: `In Progress` (Extended with Team Membership Changes)
- **Created**: `2026-10-27`

---

## 1. Context & Problem Statement

In project-based courses (`Project` topics), students work together in teams (`ProjectTeamAggregate`). Because individual effort, technical contribution, and cooperation can vary significantly within a team, teachers need a structured, transparent way for team members to evaluate themselves and their peers at specific milestones (`ProjectAssessment`).

The **Project Team Peer Assessments** feature enables:
1. **Teachers** to configure assessment windows (`OpenOnUtc` to `DeadlineUtc`) for a project and inspect detailed individual/team peer evaluation breakdowns.
2. **Students** to evaluate themselves and every teammate across three dimensions (**Cooperation**, **Contribution**, and **Effort**) on a standardized 0–5 relative scale (`AssessmentScore`), and to view their own aggregated results versus the team average once evaluations are submitted.

---

## 2. User Stories & Actors

- **Actors**: `Student`, `Teacher`

- **US-1 (Configure Project Assessment)**: As a **Teacher**, I want to create, update, or delete a `ProjectAssessment` with a description, opening timestamp, and deadline for a project so that teams can evaluate each other during a defined window.
- **US-2 (Evaluate Self & Peers)**: As a **Student**, I want to submit scores for **Cooperation**, **Contribution**, and **Effort** (plus a mandatory explanation when deviating from average) for myself and every member of my `ProjectTeam` so that our individual contributions are fairly recorded.
- **US-3 (Track Team Completion Status)**: As a **Student** or **Teacher**, I want to see whether a team's assessment is complete and which team members still need to submit evaluations.
- **US-4 (View Student Summary Result)**: As a **Student**, I want to view my own aggregated assessment result (comparing my self-assessment against my peers' average scores and the team averages, without revealing individual peer identities/explanations) so I can reflect on my performance.
- **US-5 (Inspect Detailed Teacher Results & Navigate Completed Teams)**: As a **Teacher**, I want to view the complete assessment results for a project team—including every individual peer evaluation and written explanation—and navigate seamlessly between completed teams via "Previous" and "Next" controls showing the current team name between them, skipping any incomplete teams, with a dedicated "Assessment results '[Teamname]'" tab in the course navigation, so I can grade or moderate team members fairly and efficiently.
- **US-6 (Handle Team Membership Changes Across Open and Completed Assessments)**: As a **Teacher** or **Student**, when a member joins or is removed from a `ProjectTeam`, I want any already-completed `ProjectTeamAssessment` to remain unchanged as a historical snapshot (retaining removed members and excluding newly added members), while incomplete assessments remove the leaving member's evaluations and allow the **Teacher** (in the Edit Team modal) to indicate whether a member who joined after the incomplete assessment opened (`JoinedOnUtc > OpenOnUtc`) should be included or excluded (defaulting to included if joined on/before `DeadlineUtc` and excluded if joined after `DeadlineUtc`), and future assessments automatically reflect the updated team composition.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Managing Project Assessments (Teacher)
- **Given** an existing project (`ProjectId > 0`)
- **When** a Teacher creates or updates a `ProjectAssessment` with a non-empty `Description`, `OpenOnUtc`, and `DeadlineUtc`
- **Then** the `ProjectAssessment` is persisted if `OpenOnUtc` and `DeadlineUtc` are in UTC, `OpenOnUtc < DeadlineUtc`, and (on creation) `DeadlineUtc > DateTime.UtcNow`.

### AC-2: Lazy Provisioning of a Team Assessment
- **Given** a `ProjectAssessment` that has already opened (`OpenOnUtc <= DateTime.UtcNow`) and a `ProjectTeam` with at least one eligible member
- **When** the status, peer evaluations, or results are requested for `(projectAssessmentId, teamId)` and no `ProjectTeamAssessment` exists yet
- **Then** a new `ProjectTeamAssessment` is automatically created and persisted for that assessment and team.

### AC-3: Retrieving Stored and Missing Peer Assessments for a Student
- **Given** a student who is an eligible participant of the `ProjectTeamAssessment`
- **When** the student requests their peer assessments for a `ProjectTeamAssessment`
- **Then** the system returns all stored `PeerAssessment` entries authored by that student plus newly initialized placeholder `PeerAssessment` entries (`NullScore = -1`) for any eligible team members (including self) not yet evaluated.

### AC-4: Submitting and Validating Peer Assessments
- **Given** a student who is an eligible participant of the `ProjectTeamAssessment` and `IsComplete == false`
- **When** the student saves their peer assessments for the team
- **Then** each evaluation is added or updated, provided that:
  1. All three scores (`CooperationScore`, `ContributionScore`, `EffortScore`) are valid and not `NullScore (-1)`.
  2. An `Explanation` is non-empty whenever any of the three scores is not `Average (3)`.
  3. Once the student has evaluated all $N$ eligible team members, not all members are rated strictly above `Average (3)` on the same criterion, and not all members are rated strictly below `Average (3)` on the same criterion.

### AC-5: Locking Completed Team Assessments
- **Given** a `ProjectTeamAssessment` with $N \ge 2$ participants where all $N \times N$ peer evaluations have been saved (`IsComplete == true`)
- **When** any user attempts to add or update a peer assessment
- **Then** the operation is rejected with a `ContractException`.
- **And** subsequent additions or removals of members in the underlying `ProjectTeam` do not alter `IsComplete` (`true`) or modify the stored `PeerAssessments` of the completed `ProjectTeamAssessment`.

### AC-6: Calculating Individual & Team Assessment Results
- **Given** a `ProjectTeamAssessment` where the subject has completed their self-assessment and at least one peer has evaluated the subject
- **When** an `AssessmentResult` is generated for the subject
- **Then** `AverageResult`, `EffortResult`, `ContributionResult`, and `CooperationResult` are computed with:
  - `SelfValue` / `SelfScore` (the subject's self-evaluation)
  - `PeerValue` / `PeerScore` (mean of evaluations of the subject by other teammates)
  - `Value` / `Score` (mean of all evaluations of the subject, including self)
  - `AverageValue`, `AverageSelfValue`, and `AveragePeerValue` across all participants of that `ProjectTeamAssessment`
- **And** individual `PeerAssessments` (with evaluator identity and explanation) are included **only** for Teachers (`includePeerAssessments = true`) and omitted for Students (`includePeerAssessments = false`).
- **And** for a completed `ProjectTeamAssessment`, Teacher detailed results (`GetResultsForLectorAsync`) include results for all participants who completed that assessment (derived from stored `PeerAssessments`, including members later removed from the team and excluding members who joined after completion).
- **And** on the student's personal results view (`/my-result`), only current team members who participated in the assessment can view their result, and the page title displays "[Description] - Your results".

### AC-7: Navigating Detailed Results Between Completed Teams (Teacher)
- **Given** an open or completed project assessment with multiple project teams
- **When** a Teacher navigates to the detailed results view for a team (`/detailed-results`)
- **Then** the course navigation shows "Peer assessments" as the main topic tab and displays a new active tab labeled "Assessment results '[Teamname]'" containing the current team's name.
- **And** on the detailed results view, the assessment description is displayed in the page title (e.g., "[Description] - Results"), and the current team name is clearly displayed between "Previous" and "Next" navigation controls.
- **And** the "Previous" and "Next" links navigate exclusively to adjacent teams that have completed the assessment (`IsComplete == true`), skipping any teams with incomplete assessments.
- **And** the "Previous" link is disabled when there are no prior completed teams, and the "Next" link is disabled when there are no subsequent completed teams.

### AC-8: Removing a Member from a Team with Existing Peer Assessments
- **Given** a user who is a member of a `ProjectTeam`
- **When** the user leaves or is removed from the `ProjectTeam`
- **Then** the user is removed from `ProjectTeam.TeamUsers`.
- **And** for every **incomplete** `ProjectTeamAssessment` (`IsComplete == false`) of that team (regardless of whether `DeadlineUtc` has passed):
  - All stored `PeerAssessment` entries authored by (`User.Id == userId`) or targeting (`Subject.Id == userId`) the removed user are deleted.
  - Existing evaluations submitted by remaining team members for each other are retained as-is without re-running `DR-7` relative-average validation.
  - If at least 2 eligible participants remain ($N \ge 2$) and all $N \times N$ evaluations among those remaining participants are already present, the `ProjectTeamAssessment` automatically becomes complete (`IsComplete == true`).
  - If only 1 participant remains ($N = 1$), `IsComplete` remains `false`.
- **And** for every **completed** `ProjectTeamAssessment` (`IsComplete == true`) of that team:
  - All stored `PeerAssessment` entries (including those authored by or targeting the removed user) are preserved untouched, and `IsComplete` remains `true`.
- **And** any future `ProjectTeamAssessment` created for the team excludes the removed user.

### AC-9: Adding a Member to a Team with Existing Peer Assessments & Teacher Inclusion Control
- **Given** a user who is not currently an active member (`TeamUsers`) of any `ProjectTeam` in the project (if the user is already an active member of another team, they must first leave or be removed from their current team before joining a new team)
- **When** the user joins a `ProjectTeam` and is added to `ProjectTeam.TeamUsers`
- **Then** the membership record (`ProjectTeamUser`) stores `JoinedOnUtc = DateTime.UtcNow`.
- **And** if the user was previously a member of another team in the same project and left that team after completing a `ProjectTeamAssessment` there, those historical `PeerAssessment` records in the former team's completed assessment do not prevent the user from joining the new team.
- **And** for every **completed** `ProjectTeamAssessment` (`IsComplete == true`) of that team:
  - The assessment remains completed (`IsComplete == true`) with its original participants (derived from its stored `PeerAssessments`), excluding the newly added user.
- **And** for every **incomplete** `ProjectTeamAssessment` (`IsComplete == false`) of that team:
  - **Default eligibility**:
    - If `JoinedOnUtc <= ProjectAssessment.DeadlineUtc`, the newly added user is **included by default** as an eligible participant.
    - If `JoinedOnUtc > ProjectAssessment.DeadlineUtc`, the newly added user is **excluded by default**.
  - **Teacher control in Edit Team modal**:
    - In the **Edit Team** modal (`app-project-team-edit`), for any team member who joined after an incomplete assessment opened (`JoinedOnUtc > ProjectAssessment.OpenOnUtc`), the **Teacher** can indicate (toggle) whether that member is **included in** or **excluded from** that incomplete `ProjectTeamAssessment`.
    - When the Teacher **excludes** a member from an incomplete `ProjectTeamAssessment`, all `PeerAssessment` entries already authored by (`User.Id == userId`) or targeting (`Subject.Id == userId`) that member in that assessment are deleted; if the remaining $N \ge 2$ eligible participants have all evaluated each other, the assessment automatically becomes complete (`IsComplete == true`).
    - When the Teacher **includes** a member in an incomplete `ProjectTeamAssessment`, that member becomes an eligible participant, appears in `PeersThatNeedToEvaluateOthers`, and must evaluate (and be evaluated by) all eligible teammates before `IsComplete` becomes `true`.
- **And** any future `ProjectTeamAssessment` (where `JoinedOnUtc <= ProjectAssessment.OpenOnUtc`) automatically includes the user.

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
- **DR-3 (`ProjectTeamAssessment` Creation Preconditions)**: Creating a `ProjectTeamAssessment` via `ProjectTeamAssessment.Factory.CreateNew` requires:
  - `projectAssessment != null` ("A project assessment must be provided to create a project team assessment.")
  - `projectAssessment.OpenOnUtc <= DateTime.UtcNow` ("The project team assessment cannot be created because the project assessment is not opened yet.")
  - `projectAssessment.Id > 0` ("A project team assessment can only created for an existing (stored) project assessment.")
  - `team != null` ("A team must be provided.")
  - `team.TeamUsers.Count > 0` ("The team has no members.")
  - `team.TeamUsers.First().User != null` ("The users of team must be loaded with user data.")
- **DR-4 (Eligible Participants & Team Membership Check)**:
  - For an **incomplete** `ProjectTeamAssessment` (`IsComplete == false`), the eligible participants are the members in `Team.TeamUsers` who either joined on or before `ProjectAssessment.OpenOnUtc`, or joined after `OpenOnUtc` and are currently included (by default if `JoinedOnUtc <= ProjectAssessment.DeadlineUtc`, or explicitly included/excluded by the Teacher).
  - For a **completed** `ProjectTeamAssessment` (`IsComplete == true`), the participants are derived from the distinct subjects (`Subject`) stored in `PeerAssessments`.
  - `AddOrUpdatePeerAssessment` (for both `userId` and `subjectId`), `GetPeerAssessmentsOf(userId)`, and `GetMissingPeerAssessmentsOf(userId)` require the user to be an eligible participant of the `ProjectTeamAssessment` (throwing `ContractException` if not).
- **DR-5 (Completion & Immutability After Completion)**:
  - A `ProjectTeamAssessment` is complete (`IsComplete == true`) when either:
    1. Its stored `PeerAssessments` form a complete $M \times M$ evaluation matrix among a closed set of $M \ge 2$ distinct participants (`PeerAssessments.Select(pa => pa.Subject.Id).Distinct()`) where every participant has evaluated all $M$ participants (`PeerAssessments.Count == M * M`), and if not yet locked against a changed team, matches all eligible participants at the time of completion; once complete, it remains complete even if `Team.TeamUsers` later changes.
    2. It has at least 2 participants ($N \ge 2$); a single-member team ($N < 2$) can never be marked `IsComplete == true`.
  - Once `IsComplete == true`, no peer assessment in that `ProjectTeamAssessment` can be added, modified, or deleted (`Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to change a peer assessment.")`).
- **DR-6 (`AddOrUpdatePeerAssessment` Score & Explanation Preconditions)**:
  - Empty (`NullScore = -1`) scores are forbidden on save:
    - `cooperationScore != AssessmentScore.NullScore` ("An empty score for 'cooperation' is not allowed.")
    - `contributionScore != AssessmentScore.NullScore` ("An empty score for 'contribution' is not allowed.")
    - `effortScore != AssessmentScore.NullScore` ("An empty score for 'effort' is not allowed.")
  - If `cooperationScore != AssessmentScore.Average (3)` OR `contributionScore != AssessmentScore.Average (3)` OR `effortScore != AssessmentScore.Average (3)`, `explanation` must not be null, empty, or whitespace (`!string.IsNullOrWhiteSpace(explanation)`, "An explanation is required when a score is not 'Equal to group average' (3).").
- **DR-7 (Zero-Sum / Relative Average Guard in `ValidateAssessmentsOf`)**: When a user saves their evaluations and has evaluated all eligible team members (`allAssessmentsOfUser.Count == eligibleCount`), they cannot rate **every** member $> 3$ (`Average`) on `ContributionScore`, `CooperationScore`, or `EffortScore`, nor can they rate **every** member $< 3$ (`Average`) on `ContributionScore`, `CooperationScore`, or `EffortScore` (each violation throws a `ContractException`). If the user has not yet evaluated all eligible team members, `ValidateAssessmentsOf` performs no check. This validation is enforced on save and is not retroactively re-run when another member leaves or is excluded from an incomplete assessment.
- **DR-8 (Result Calculation Preconditions)**: Creating an `AssessmentResult` for a subject requires the subject to be a participant of the `ProjectTeamAssessment` (either in `Team.TeamUsers` or in stored `PeerAssessments` of a completed assessment), with both a self-assessment by the subject and at least one peer assessment from another participant.
- **DR-9 (`PeerAssessment` Construction & Identity Invariants)**:
  - Constructing a `PeerAssessment(int projectTeamAssessmentId, User user, User subject)` requires:
    - `projectTeamAssessmentId > 0` ("An existing project team assessment identifier must be provided.")
    - `user != null` ("A user must be provided.") and `user.Id > 0` ("An existing user must be provided.")
    - `subject != null` ("A subject must be provided.") and `subject.Id > 0` ("An existing subject must be provided.")
  - Upon construction, `CooperationScore`, `ContributionScore`, and `EffortScore` are initialized to `AssessmentScore.NullScore (-1)`, and `IsSelfAssessment` is `true` when `User.Id == Subject.Id`.
- **DR-10 (`ProjectTeamUser.JoinedOnUtc`)**: Every `ProjectTeamUser` tracks `JoinedOnUtc` (in UTC) indicating when the user joined the `ProjectTeam`.
- **DR-11 (Selective Cleanup on Team Member Removal or Exclusion)**: Removing a user from a `ProjectTeam` (or a Teacher excluding a late-joining member from an incomplete assessment) deletes `PeerAssessment` records involving that user (`User.Id == userId || Subject.Id == userId`) **only** for the affected `ProjectTeamAssessment` instances where `IsComplete == false`.
- **DR-12 (Single Active Team Membership per Project)**: A student can only be an active member (`TeamUsers`) of **one** `ProjectTeam` within a project at any time. To join another team in the same project, the student must first leave (or be removed from) their current team.

---

## 5. Authorization & Security Rules

- **Students**:
  - Can view `ProjectAssessment` metadata (`GET /api/project-assessments/...`).
  - Can view team assessment completion status (`GET .../status`).
  - Can view and save **only their own** authored peer assessments (`dto.UserId == authenticatedUserId`) when they are an eligible participant of the assessment.
  - Can view **only their own** aggregated summary result (`GET .../my-result`), provided they are currently a member of the `ProjectTeam` (`Team.TeamUsers`) and participated in the assessment; removed team members cannot access the team's assessment results. Individual peer evaluations are excluded (`PeerAssessments` collection is empty).
- **Teachers (`ApiConstants.LectorsOnlyPolicy`)**:
  - Exclusive permission to create (`POST`), update (`PUT`), and delete (`DELETE`) `ProjectAssessment` definitions.
  - Exclusive permission to include or exclude late-joining team members (`JoinedOnUtc > OpenOnUtc`) in an incomplete `ProjectTeamAssessment` via the Edit Team modal.
  - Exclusive permission to view detailed team results (`GET .../detailed-results`), which include raw individual peer evaluations, explanations, and results for all participants of that assessment (including participants who were later removed from the team).

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Creating team assessment with null `projectAssessment`, unpersisted `projectAssessment` (`Id <= 0`), or before `OpenOnUtc` | Throws `ContractException` | `400 Bad Request` |
| Creating team assessment with null `team`, empty `team.TeamUsers`, or unloaded `User` data | Throws `ContractException` | `400 Bad Request` |
| Creating `PeerAssessment` with `projectTeamAssessmentId <= 0`, null/unpersisted `user`, or null/unpersisted `subject` | Throws `ContractException` | `400 Bad Request` |
| Submitting `NullScore (-1)` for Cooperation, Contribution, or Effort | Throws `ContractException` | `400 Bad Request` |
| Non-Average score submitted without explanation (null, empty, or whitespace) | Throws `ContractException` | `400 Bad Request` |
| Student rates all teammates above (or all below) Average on Cooperation, Contribution, or Effort on save | Throws `ContractException` in `ValidateAssessmentsOf` | `400 Bad Request` |
| Non-member / non-participant userId or subjectId passed to `AddOrUpdatePeerAssessment`, `GetPeerAssessmentsOf`, or `GetMissingPeerAssessmentsOf` | Throws `ContractException` | `400 Bad Request` |
| Student attempts to save assessments for another `userId` | Throws `ContractException` | `400 Bad Request` |
| Modifying assessments after `IsComplete == true` | Throws `ContractException` | `400 Bad Request` |
| Requesting results before self or peers have evaluated | Throws `ContractException` | `400 Bad Request` |
| Student attempts to join a team while still an active member of another team in the project | Throws `ContractException` ("The user is already a member of '...'. It is not allowed to be in multiple teams.") | `400 Bad Request` |
| Removed team member requests `/my-result` for a completed assessment | Throws `ContractException` (user is no longer a member of `Team.TeamUsers`) | `400 Bad Request` |
| Excluded late-joining member attempts to evaluate in an incomplete assessment | Throws `ContractException` (user is not an eligible participant of the assessment) | `400 Bad Request` |
| Teacher excludes a late-joining member from an incomplete assessment after evaluations were saved for them | That member's `PeerAssessment` entries in the incomplete assessment are deleted; if remaining $N \ge 2$ eligible members have evaluated each other, `IsComplete` becomes `true` | `200 OK` |
| Teacher attempts to change member inclusion on a completed assessment (`IsComplete == true`) | Throws `ContractException` | `400 Bad Request` |
| Member leaves a 2-person incomplete assessment where the remaining member already evaluated both | `PeerAssessment` for leaving member is deleted; 1 self-assessment remains ($N=1$), `IsComplete` stays `false` | `200 OK` |
| Member leaves a 3-person incomplete assessment where the remaining 2 members already evaluated everyone | Leaving member's `PeerAssessment` entries are deleted; remaining $2 \times 2 = 4$ evaluations auto-complete the assessment (`IsComplete == true`) | `200 OK` |
| Unknown `projectAssessmentId` or `teamId` when creating team assessment | `DataNotFoundException` caught and rethrown as `ContractException` | `400 Bad Request` |

---

## 7. Out of Scope

- Direct automatic synchronization of peer assessment scores into `ExamAggregate` grades (handled separately via exam/score administration).
- Email/notification reminders for pending peer assessments.

---

## 8. Open Questions

- None.

