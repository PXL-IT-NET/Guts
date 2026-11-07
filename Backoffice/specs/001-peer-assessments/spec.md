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
- **US-6 (Manage Assessment Peers & Handle Team Membership Changes)**: As a **Teacher** or **Student**, I want each `ProjectTeamAssessment` to maintain its own explicit `Peers` collection (initially copied from `Team.TeamUsers` upon creation) so that:
  1. Completed assessments (`IsComplete == true`) are permanently locked (subsequent team joins, leaves, or Teacher toggles do not alter `Peers`, `IsComplete`, or stored `PeerAssessments`).
  2. When a member leaves or is removed from a `ProjectTeam`, they are automatically removed from `Peers` (along with their evaluations) in any **incomplete** `ProjectTeamAssessment` (`IsComplete == false`).
  3. When a member joins a `ProjectTeam`, they are automatically added to `Peers` of any existing **incomplete** `ProjectTeamAssessment` if `DateTime.UtcNow <= ProjectAssessment.DeadlineUtc` (and not added automatically if `DateTime.UtcNow > ProjectAssessment.DeadlineUtc` or `IsComplete == true`).
  4. In the **Edit Team** modal (`app-project-team-edit`), a **Teacher** can include or exclude any current team member (`Team.TeamUsers`) as a peer in any open **incomplete** `ProjectTeamAssessment` (`IsComplete == false`).

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Managing Project Assessments (Teacher)
- **Given** an existing project (`ProjectId > 0`)
- **When** a Teacher creates or updates a `ProjectAssessment` with a non-empty `Description`, `OpenOnUtc`, and `DeadlineUtc`
- **Then** the `ProjectAssessment` is persisted if `OpenOnUtc` and `DeadlineUtc` are in UTC, `OpenOnUtc < DeadlineUtc`, and (on creation) `DeadlineUtc > DateTime.UtcNow`.

### AC-2: Lazy Provisioning of a Team Assessment
- **Given** a `ProjectAssessment` that has already opened (`OpenOnUtc <= DateTime.UtcNow`) and a `ProjectTeam` with at least one member
- **When** the status, peer evaluations, or results are requested for `(projectAssessmentId, teamId)` and no `ProjectTeamAssessment` exists yet
- **Then** a new `ProjectTeamAssessment` is automatically created with its `Peers` collection initialized by copying the current users of `Team.TeamUsers`, and persisted for that assessment and team.

### AC-3: Retrieving Stored and Missing Peer Assessments for a Student
- **Given** a student who is in `ProjectTeamAssessment.Peers`
- **When** the student requests their peer assessments for a `ProjectTeamAssessment`
- **Then** the system returns all stored `PeerAssessment` entries authored by that student plus newly initialized placeholder `PeerAssessment` entries (`NullScore = -1`) for any peer in `Peers` (including self) not yet evaluated.

### AC-4: Submitting and Validating Peer Assessments
- **Given** a student who is in `ProjectTeamAssessment.Peers` and `IsComplete == false`
- **When** the student saves their peer assessments for the team
- **Then** each evaluation is added or updated, provided that:
  1. All three scores (`CooperationScore`, `ContributionScore`, `EffortScore`) are valid and not `NullScore (-1)`.
  2. An `Explanation` is non-empty whenever any of the three scores is not `Average (3)`.
  3. Once the student has evaluated all $N$ peers in `Peers`, not all peers are rated strictly above `Average (3)` on the same criterion, and not all peers are rated strictly below `Average (3)` on the same criterion.

### AC-5: Locking Completed Team Assessments
- **Given** a `ProjectTeamAssessment` with $N \ge 2$ peers in `Peers` where all $N \times N$ peer evaluations have been saved (`IsComplete == true`)
- **When** any user attempts to add or update a peer assessment, or add/remove a peer in `Peers`
- **Then** the operation is rejected with a `ContractException`.
- **And** subsequent additions or removals of members in the underlying `ProjectTeam` do not alter `Peers`, `IsComplete` (`true`), or the stored `PeerAssessments` of the completed `ProjectTeamAssessment`.

### AC-6: Calculating Individual & Team Assessment Results
- **Given** a `ProjectTeamAssessment` where the subject has completed their self-assessment and at least one peer has evaluated the subject
- **When** an `AssessmentResult` is generated for the subject
- **Then** `AverageResult`, `EffortResult`, `ContributionResult`, and `CooperationResult` are computed with:
  - `SelfValue` / `SelfScore` (the subject's self-evaluation)
  - `PeerValue` / `PeerScore` (mean of evaluations of the subject by other teammates)
  - `Value` / `Score` (mean of all evaluations of the subject, including self)
  - `AverageValue`, `AverageSelfValue`, and `AveragePeerValue` across all peers in `ProjectTeamAssessment.Peers`
- **And** individual `PeerAssessments` (with evaluator identity and explanation) are included **only** for Teachers (`includePeerAssessments = true`) and omitted for Students (`includePeerAssessments = false`).
- **And** for a completed `ProjectTeamAssessment`, Teacher detailed results (`GetResultsForTeacherAsync`) include results for all peers in `ProjectTeamAssessment.Peers` (including members later removed from `Team.TeamUsers` and excluding members who joined after completion).
- **And** on the student's personal results view (`/my-result`), only current team members (`Team.TeamUsers`) who are in `ProjectTeamAssessment.Peers` can view their result, and the page title displays "[Description] - Your results".

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
  - The user is removed from `ProjectTeamAssessment.Peers`.
  - All stored `PeerAssessment` entries authored by (`User.Id == userId`) or targeting (`Subject.Id == userId`) the removed user are deleted.
  - Existing evaluations submitted by remaining peers in `Peers` for each other are retained as-is without re-running `DR-7` relative-average validation.
  - If at least 2 peers remain in `Peers` ($N \ge 2$) and all $N \times N$ evaluations among those remaining peers are already present, the `ProjectTeamAssessment` automatically becomes complete (`IsComplete == true`) and locked.
  - If fewer than 2 peers remain in `Peers` ($N < 2$), `IsComplete` remains `false`.
- **And** for every **completed** `ProjectTeamAssessment` (`IsComplete == true`) of that team:
  - Both `ProjectTeamAssessment.Peers` and all stored `PeerAssessment` entries (including those authored by or targeting the removed user) are preserved untouched, and `IsComplete` remains `true`.
- **And** any future `ProjectTeamAssessment` created for the team copies only the current `Team.TeamUsers` into `Peers`, excluding the removed user.

### AC-9: Adding a Member to a Team & Teacher Peer Inclusion Control
- **Given** a user who is not currently an active member (`TeamUsers`) of any `ProjectTeam` in the project (if the user is already an active member of another team, they must first leave or be removed from their current team before joining a new team)
- **When** the user joins a `ProjectTeam` and is added to `ProjectTeam.TeamUsers`
- **Then** if the user was previously a member of another team in the same project and left that team after completing a `ProjectTeamAssessment` there, those historical `Peers` and `PeerAssessment` records in the former team's completed assessment do not prevent the user from joining the new team.
- **And** for every **completed** `ProjectTeamAssessment` (`IsComplete == true`) of that team:
  - The assessment remains completed (`IsComplete == true`) with its existing `Peers` collection unchanged, excluding the newly added user.
- **And** for every **incomplete** `ProjectTeamAssessment` (`IsComplete == false`) of that team that already exists:
  - **Default automatic inclusion on join**:
    - If `DateTime.UtcNow <= ProjectAssessment.DeadlineUtc`, the newly added user is **automatically added** to `ProjectTeamAssessment.Peers`.
    - If `DateTime.UtcNow > ProjectAssessment.DeadlineUtc`, the newly added user is **not automatically added** to `ProjectTeamAssessment.Peers`.
- **And** in the **Edit Team** modal (`app-project-team-edit`), for any open **incomplete** `ProjectTeamAssessment` (`IsComplete == false`), the **Teacher** can include or exclude **any** current member of `Team.TeamUsers` in `ProjectTeamAssessment.Peers`:
  - When the Teacher **excludes** (`RemovePeer(userId)`) a team member from an incomplete `ProjectTeamAssessment`, the user is removed from `Peers` and all `PeerAssessment` entries authored by (`User.Id == userId`) or targeting (`Subject.Id == userId`) that member in that assessment are deleted; if the remaining $N \ge 2$ peers in `Peers` have all evaluated each other, the assessment automatically becomes complete (`IsComplete == true`) and locked.
  - When the Teacher **includes** (`AddPeer(userId)`) a team member in an incomplete `ProjectTeamAssessment`, that member is added to `Peers`, appears in `PeersThatNeedToEvaluateOthers`, and must evaluate (and be evaluated by) all peers in `Peers` before `IsComplete` becomes `true`.
- **And** any future `ProjectTeamAssessment` created for the team copies all current `Team.TeamUsers` into `Peers`.

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
- **DR-3 (`ProjectTeamAssessment` Creation Preconditions & Initial `Peers`)**: Creating a `ProjectTeamAssessment` via `ProjectTeamAssessment.Factory.CreateNew` requires:
  - `projectAssessment != null` ("A project assessment must be provided to create a project team assessment.")
  - `projectAssessment.OpenOnUtc <= DateTime.UtcNow` ("The project team assessment cannot be created because the project assessment is not opened yet.")
  - `projectAssessment.Id > 0` ("A project team assessment can only created for an existing (stored) project assessment.")
  - `team != null` ("A team must be provided.")
  - `team.TeamUsers.Count > 0` ("The team has no members.")
  - `team.TeamUsers.First().User != null` ("The users of team must be loaded with user data.")
  - Upon creation, `ProjectTeamAssessment.Peers` is initialized by copying all `User` entities from `team.TeamUsers`.
- **DR-4 (`Peers` Collection & Peer Lookup)**:
  - `ProjectTeamAssessment.Peers` (`IReadOnlyCollection<User>`) is the single source of truth for the participants of that `ProjectTeamAssessment`.
  - `GetPeer(int userId)` returns the `User` in `Peers` matching `userId`, or throws a `ContractException` if the user is not in `Peers`.
  - `AddOrUpdatePeerAssessment` (for both `userId` and `subjectId`), `GetPeerAssessmentsOf(userId)`, and `GetMissingPeerAssessmentsOf(userId)` require the user to be in `Peers` via `GetPeer(...)` (throwing `ContractException` if not).
  - Adding a peer (`AddPeer(int userId)`) requires `!IsComplete` and requires the user to be a current member of `Team.TeamUsers` (`Team.GetTeamUser(userId)`).
  - Removing a peer (`RemovePeer(int userId)`) requires `!IsComplete`, removes the user from `Peers`, and deletes any `PeerAssessment` in `PeerAssessments` where `User.Id == userId || Subject.Id == userId`.
- **DR-5 (Completion & Immutability After Completion)**:
  - A `ProjectTeamAssessment` is complete (`IsComplete == true`) strictly when `Peers.Count >= 2 && PeerAssessments.Count == Peers.Count * Peers.Count` (at least 2 peers in `Peers`, and every peer in `Peers` has evaluated every peer in `Peers`). A team assessment with fewer than 2 peers ($N < 2$) can never be `IsComplete == true`.
  - Once `IsComplete == true`, the assessment is locked: no peer assessment can be added, modified, or deleted (`Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to change a peer assessment.")`), and no peer can be added to or removed from `Peers` (`Contracts.Require(!IsComplete, "The team assessment is completed by all peers. It is not possible anymore to add or remove peers.")`).
- **DR-6 (`AddOrUpdatePeerAssessment` Score & Explanation Preconditions)**:
  - Empty (`NullScore = -1`) scores are forbidden on save:
    - `cooperationScore != AssessmentScore.NullScore` ("An empty score for 'cooperation' is not allowed.")
    - `contributionScore != AssessmentScore.NullScore` ("An empty score for 'contribution' is not allowed.")
    - `effortScore != AssessmentScore.NullScore` ("An empty score for 'effort' is not allowed.")
  - If `cooperationScore != AssessmentScore.Average (3)` OR `contributionScore != AssessmentScore.Average (3)` OR `effortScore != AssessmentScore.Average (3)`, `explanation` must not be null, empty, or whitespace (`!string.IsNullOrWhiteSpace(explanation)`, "An explanation is required when a score is not 'Equal to group average' (3).").
- **DR-7 (Zero-Sum / Relative Average Guard in `ValidateAssessmentsOf`)**: When a user saves their evaluations and has evaluated all peers in `Peers` (`allAssessmentsOfUser.Count == Peers.Count`), they cannot rate **every** peer $> 3$ (`Average`) on `ContributionScore`, `CooperationScore`, or `EffortScore`, nor can they rate **every** peer $< 3$ (`Average`) on `ContributionScore`, `CooperationScore`, or `EffortScore` (each violation throws a `ContractException`). If the user has not yet evaluated all peers in `Peers`, `ValidateAssessmentsOf` performs no check. This validation is enforced on save and is not retroactively re-run when another member leaves or is excluded from an incomplete assessment.
- **DR-8 (Result Calculation Preconditions)**: Creating an `AssessmentResult` for a subject requires:
  - When `includePeerAssessments == false` (Student requesting `/my-result`): the subject must be a current member of `Team.TeamUsers` (`Team.GetTeamUser(subjectId)`) AND a peer in `ProjectTeamAssessment.Peers` (`GetPeer(subjectId)`).
  - When `includePeerAssessments == true` (Teacher requesting `/detailed-results`): the subject must be a peer in `ProjectTeamAssessment.Peers` (`GetPeer(subjectId)`).
  - Both a self-assessment by the subject and at least one peer assessment from another peer must exist.
- **DR-9 (`PeerAssessment` Construction & Identity Invariants)**:
  - Constructing a `PeerAssessment(int projectTeamAssessmentId, User user, User subject)` requires:
    - `projectTeamAssessmentId > 0` ("An existing project team assessment identifier must be provided.")
    - `user != null` ("A user must be provided.") and `user.Id > 0` ("An existing user must be provided.")
    - `subject != null` ("A subject must be provided.") and `subject.Id > 0` ("An existing subject must be provided.")
  - Upon construction, `CooperationScore`, `ContributionScore`, and `EffortScore` are initialized to `AssessmentScore.NullScore (-1)`, and `IsSelfAssessment` is `true` when `User.Id == Subject.Id`.
- **DR-10 (Automatic `Peers` Synchronization on Team Join/Leave)**:
  - When a user joins a `ProjectTeam`, for each existing `ProjectTeamAssessment` of that team where `!IsComplete && DateTime.UtcNow <= ProjectAssessment.DeadlineUtc`, the user is automatically added to `Peers` (`AddPeer(userId)`).
  - When a user leaves or is removed from a `ProjectTeam`, for each existing `ProjectTeamAssessment` of that team where `!IsComplete`, the user is automatically removed from `Peers` (`RemovePeer(userId)`) along with their `PeerAssessment` entries; completed assessments (`IsComplete == true`) are left untouched.
- **DR-11 (Single Active Team Membership per Project)**: A student can only be an active member (`TeamUsers`) of **one** `ProjectTeam` within a project at any time. To join another team in the same project, the student must first leave (or be removed from) their current team.

---

## 5. Authorization & Security Rules

- **Students**:
  - Can view `ProjectAssessment` metadata (`GET /api/project-assessments/...`).
  - Can view team assessment completion status (`GET .../status`).
  - Can view and save **only their own** authored peer assessments (`dto.UserId == authenticatedUserId`) when they are in `ProjectTeamAssessment.Peers`.
  - Can view **only their own** aggregated summary result (`GET .../my-result`), provided they are currently a member of the `ProjectTeam` (`Team.TeamUsers`) and in `ProjectTeamAssessment.Peers`; removed team members cannot access the team's assessment results. Individual peer evaluations are excluded (`PeerAssessments` collection is empty).
- **Teachers (`ApiConstants.LectorsOnlyPolicy`)**:
  - Exclusive permission to create (`POST`), update (`PUT`), and delete (`DELETE`) `ProjectAssessment` definitions.
  - Exclusive permission to include or exclude any current team member (`Team.TeamUsers`) in an incomplete `ProjectTeamAssessment.Peers` collection via the Edit Team modal.
  - Exclusive permission to view detailed team results (`GET .../detailed-results`), which include raw individual peer evaluations, explanations, and results for all peers in `ProjectTeamAssessment.Peers` (including peers who were later removed from `Team.TeamUsers`).

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
| User not in `ProjectTeamAssessment.Peers` passed to `AddOrUpdatePeerAssessment`, `GetPeerAssessmentsOf`, or `GetMissingPeerAssessmentsOf` | Throws `ContractException` | `400 Bad Request` |
| Student attempts to save assessments for another `userId` | Throws `ContractException` | `400 Bad Request` |
| Modifying assessments or adding/removing peers after `IsComplete == true` | Throws `ContractException` | `400 Bad Request` |
| Requesting results before self or peers have evaluated | Throws `ContractException` | `400 Bad Request` |
| Student attempts to join a team while still an active member of another team in the project | Throws `ContractException` ("The user is already a member of '...'. It is not allowed to be in multiple teams.") | `400 Bad Request` |
| Removed team member requests `/my-result` for a completed assessment | Throws `ContractException` (user is no longer a member of `Team.TeamUsers`) | `400 Bad Request` |
| Team member excluded from `Peers` attempts to evaluate in an incomplete assessment | Throws `ContractException` (user is not a peer in this team assessment) | `400 Bad Request` |
| Teacher excludes a team member from an incomplete assessment after evaluations were saved for them | Member is removed from `Peers` and their `PeerAssessment` entries are deleted; if remaining $N \ge 2$ peers in `Peers` have evaluated each other, `IsComplete` becomes `true` and locked | `200 OK` |
| Teacher attempts to change peer inclusion on a completed assessment (`IsComplete == true`) | Throws `ContractException` | `400 Bad Request` |
| Member leaves a 2-person incomplete assessment where the remaining member already evaluated both | Member is removed from `Peers` and their `PeerAssessment` is deleted; 1 self-assessment remains ($N=1$), `IsComplete` stays `false` | `200 OK` |
| Member leaves a 3-person incomplete assessment where the remaining 2 members already evaluated everyone | Member is removed from `Peers` and their `PeerAssessment` entries are deleted; remaining $2 \times 2 = 4$ evaluations auto-complete the assessment (`IsComplete == true`) | `200 OK` |
| Unknown `projectAssessmentId` or `teamId` when creating team assessment | `DataNotFoundException` caught and rethrown as `ContractException` | `400 Bad Request` |

---

## 7. Out of Scope

- Direct automatic synchronization of peer assessment scores into `ExamAggregate` grades (handled separately via exam/score administration).
- Email/notification reminders for pending peer assessments.

---

## 8. Open Questions

- None.

