# TDD Implementation Plan: Project Team Peer Assessments — Team Membership Changes

- **Feature ID**: `001-peer-assessments`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-9, DR-10]` Update `IProjectTeamUser`, `ProjectTeamUser`, and test builders (`ProjectTeamMockBuilder`, `ProjectAssessmentMockBuilder`, `ProjectTeamAssessmentBuilder`) to support `JoinedOnUtc` (defaulting to UTC `DateTime.MinValue` for existing/seeded members) and `DeadlineUtc`.
- [x] **[T1.2]** `[AC-3, AC-4, AC-5, AC-8, AC-9, DR-4, DR-5, DR-6, DR-7, DR-9, DR-10, DR-11]` Write failing unit tests in `Guts.Domain.Tests/ProjectTeamAssessmentAggregate/ProjectTeamAssessmentTests.cs` covering:
  - Completed assessment (`IsComplete == true`, $N \ge 2$) locking `CompletedOnUtc` and preserving `IsComplete == true` and `GetPeers()` when members later join or leave `Team.TeamUsers`.
  - Default eligibility of late-joining members (`JoinedOnUtc > OpenOnUtc`): included by default when `JoinedOnUtc <= DeadlineUtc`, excluded by default when `JoinedOnUtc > DeadlineUtc`.
  - Teacher inclusion override via `SetPeerInclusion(userId, isIncluded)` for late-joining members (`JoinedOnUtc > OpenOnUtc`), including deleting stored `PeerAssessment` records when `isIncluded == false`, auto-completing when remaining $N \ge 2$ eligible peers have evaluated each other, and throwing `ContractException` when `IsComplete == true`, user is not in team, or user joined on/before `OpenOnUtc`.
  - `RemovePeer(userId)`: leaving completed assessments untouched, deleting `PeerAssessment` entries for incomplete assessments, auto-completing when $N \ge 2$ remaining eligible peers have evaluated each other, and keeping `IsComplete == false` when only 1 peer remains ($N = 1$).
- [x] **[T1.3]** `[AC-6, DR-8]` Write failing unit tests in `Guts.Domain.Tests/AssessmentResultAggregate/AssessmentResultFactoryTests.cs` covering:
  - Creating a Teacher result (`includePeerAssessments: true`) for a peer of a completed assessment who was subsequently removed from `Team.TeamUsers`.
  - Rejecting a Student result (`includePeerAssessments: false`) with `ContractException` when the user was removed from `Team.TeamUsers`.
- [x] **[T1.4]** `[AC-3, AC-4, AC-5, AC-6, AC-8, AC-9, DR-4, DR-5, DR-8, DR-10, DR-11]` Implement domain logic in `IProjectTeamAssessment.cs`, `ProjectTeamAssessment.cs`, and `AssessmentResult.cs`.
- [x] **[T1.5]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Business Layer (`Guts.Business.Tests` ➔ `Guts.Business`)

- [x] **[T2.1]** `[AC-6, AC-9]` Update `ProjectTeamAssessmentStatusDto` (add `PeerIds`) and `IProjectTeamAssessmentService` (add `SetPeerInclusionAsync`).
- [x] **[T2.2]** `[AC-6, AC-9, DR-12]` Write failing unit tests in `Guts.Business.Tests/Services/Assessment/ProjectTeamAssessmentServiceTests.cs` (and verify `ProjectServiceTests.cs` single-active-team check) covering:
  - `GetStatusAsync` returning `PeerIds` from `GetPeers()`.
  - `GetResultsForTeacherAsync` iterating over `teamAssessment.GetPeers()` (including removed members of a completed assessment and excluding members who joined after completion).
  - `SetPeerInclusionAsync` loading the team assessment, invoking `SetPeerInclusion(userId, isIncluded)`, and persisting via `repository.UpdateAsync(...)`.
- [x] **[T2.3]** `[AC-6, AC-9]` Implement `ProjectTeamAssessmentService` updates in `Guts.Business/Services/Assessment/ProjectTeamAssessmentService.cs`.
- [x] **[T2.4]** Run `dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [ ] **[T3.1]** `[AC-5, AC-8, AC-9, DR-10]` Update EF Core configurations (`ProjectTeamConfiguration.cs`, `ProjectTeamAssessmentConfiguration.cs`) to map `ProjectTeamUser.JoinedOnUtc` and `ProjectTeamAssessment` (`CompletedOnUtc`, `_includedUserIds`, `_excludedUserIds`), and add an EF Core migration.
- [ ] **[T3.2]** `[AC-5, AC-8, AC-9, DR-10, DR-11]` Write failing repository tests in `Guts.Infrastructure.Tests/Repositories/ProjectTeamDbRepositoryTests.cs` and `ProjectTeamAssessmentDbRepositoryTests.cs` covering:
  - `AddUserToTeam` setting `JoinedOnUtc` to current UTC timestamp.
  - `RemoveUserFromTeam` deleting `PeerAssessment` entries from incomplete `ProjectTeamAssessment` instances while preserving `PeerAssessment` entries in completed `ProjectTeamAssessment` instances.
  - `ProjectTeamAssessmentDbRepository.LoadAsync` loading `PeerAssessments.Subject` as well as `CompletedOnUtc`, `_includedUserIds`, and `_excludedUserIds`, and `UpdateAsync` deleting removed `PeerAssessment` entities when a peer is excluded.
- [ ] **[T3.3]** `[AC-5, AC-8, AC-9, DR-10, DR-11]` Implement updates in `ProjectTeamDbRepository.cs` and `ProjectTeamAssessmentDbRepository.cs`.
- [ ] **[T3.4]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [ ] **[T4.1]** `[AC-9]` Write failing unit tests in `Guts.Api.Tests/Controllers/ProjectTeamAssessmentControllerTests.cs` and `Guts.Api.Tests/Models/Converters/TeamConverterTests.cs` covering:
  - `PUT of-project-assessment/{projectAssessmentId}/of-team/{teamId}/peers/{userId}/inclusion` calling `SetPeerInclusionAsync` and returning `OkResult`.
  - `TeamConverter.ToTeamDetailsModel` mapping `JoinedOnUtc` onto `TeamUserModel`.
- [ ] **[T4.2]** `[AC-9]` Implement `TeamUserModel.JoinedOnUtc`, `TeamConverter`, and `ProjectTeamAssessmentController.SetPeerInclusion` in `Guts.Api/`.
- [ ] **[T4.3]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [ ] **[T5.1]** `[AC-8, AC-9]` Write failing Jasmine unit tests (`*.spec.ts`) in `project.teamassessment.service.spec.ts` and `project-team-edit.component.spec.ts` covering:
  - `ProjectTeamAssessmentService.setPeerInclusion(...)` sending `PUT` request.
  - `ProjectTeamEditComponent` displaying inclusion checkboxes for late-joining team members (`joinedOnUtc > openOnUtc`) on incomplete assessments and invoking `setPeerInclusion` when toggled.
- [ ] **[T5.2]** `[AC-8, AC-9]` Implement updates in `team.model.ts`, `projectassessment.model.ts`, `project.teamassessment.service.ts`, `project-team.component.ts/.html`, and `project-team-edit.component.ts/.html`.
- [ ] **[T5.3]** Run `npm test -- --watch=false` in `Backoffice/Guts.Web/ClientApp` and verify all unit tests pass.
- [ ] **[T5.4]** `[AC-5, AC-6, AC-8, AC-9, DR-12]` Write Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/` covering:
  - Student leaving a team and joining another team (`DR-12`, `AC-8`, `AC-9`).
  - Removing a member from a team with an incomplete vs. completed peer assessment (`AC-5`, `AC-6`, `AC-8`).
  - New member joining a team during an incomplete peer assessment and Teacher toggling their inclusion in the Edit Team modal (`AC-9`).
- [ ] **[T5.5]** Run Playwright E2E tests (`npm run test:e2e` in `Backoffice/Guts.Web/ClientApp`) and verify all E2E scenarios pass.

---

## Phase 6: Final Verification & Quality Gate

- [ ] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [ ] **[T6.2]** Run frontend production build & test check: `npm run build`
- [ ] **[T6.3]** Run SonarQube analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and resolve any reported bugs, code smells, or coverage gaps.
- [ ] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
