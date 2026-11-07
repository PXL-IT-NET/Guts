# TDD Implementation Plan: Project Team Peer Assessments — Explicit Peers Collection & Membership Changes

- **Feature ID**: `001-peer-assessments`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-2, AC-3, AC-4, AC-5, AC-8, AC-9, DR-3, DR-4, DR-5, DR-7]` Write failing unit tests in `Guts.Domain.Tests/ProjectTeamAssessmentAggregate/ProjectTeamAssessmentTests.cs` covering:
  - Creating a `ProjectTeamAssessment` initializes `Peers` by copying all users from `Team.TeamUsers`.
  - `IsComplete` requires `Peers.Count >= 2 && PeerAssessments.Count == Peers.Count * Peers.Count` (a 1-peer assessment is never `IsComplete == true`).
  - Once `IsComplete == true`, `Peers` and `PeerAssessments` are locked: `AddPeer`, `RemovePeer`, `SetPeerInclusion`, and `AddOrUpdatePeerAssessment` throw `ContractException`.
  - `AddPeer(userId)` / `SetPeerInclusion(userId, true)` adds a current member of `Team.TeamUsers` to `Peers` when `!IsComplete`, and throws `ContractException` if the user is not in `Team.TeamUsers` or if `IsComplete == true`.
  - `RemovePeer(userId)` / `SetPeerInclusion(userId, false)` removes the user from `Peers` and deletes any `PeerAssessment` entries authored by or targeting `userId` when `!IsComplete` (auto-completing the assessment if the remaining $N \ge 2$ peers in `Peers` have evaluated each other, or keeping `IsComplete == false` if $N < 2$).
- [x] **[T1.2]** `[AC-6, DR-8]` Write failing unit tests in `Guts.Domain.Tests/AssessmentResultAggregate/AssessmentResultFactoryTests.cs` covering:
  - Creating a Teacher result (`includePeerAssessments: true`) for a peer in `ProjectTeamAssessment.Peers` even after the user was removed from `Team.TeamUsers`.
  - Rejecting a Student result (`includePeerAssessments: false`) with `ContractException` when the user was removed from `Team.TeamUsers` (or is not in `Peers`).
- [x] **[T1.3]** `[AC-2, AC-3, AC-4, AC-5, AC-6, AC-8, AC-9, DR-3, DR-4, DR-5, DR-8]` Implement `Peers`, `GetPeer`, `AddPeer`, `RemovePeer`, and `SetPeerInclusion` in `IProjectTeamAssessment.cs`, `ProjectTeamAssessment.cs`, `ProjectTeamAssessmentBuilder.cs`, and `AssessmentResult.cs`.
- [x] **[T1.4]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Business Layer (`Guts.Business.Tests` ➔ `Guts.Business`)

- [x] **[T2.1]** `[AC-6, AC-9]` Update `ProjectTeamAssessmentStatusDto` (add `PeerIds`) and `IProjectTeamAssessmentService` (add `SetPeerInclusionAsync`).
- [x] **[T2.2]** `[AC-6, AC-9]` Write failing unit tests in `Guts.Business.Tests/Services/Assessment/AssignmentServiceTests.cs` (`ProjectTeamAssessmentServiceTests`) covering:
  - `GetStatusAsync` returning `PeerIds` from `teamAssessment.Peers`.
  - `GetResultsForTeacherAsync` iterating over `teamAssessment.Peers` (including removed team members who are in `Peers` and excluding non-peer team members).
  - `SetPeerInclusionAsync` loading the team assessment, invoking `SetPeerInclusion(userId, isIncluded)`, and persisting via `repository.UpdateAsync(...)`.
- [x] **[T2.3]** `[AC-6, AC-9]` Implement `ProjectTeamAssessmentService` updates in `Guts.Business/Services/Assessment/ProjectTeamAssessmentService.cs`.
- [x] **[T2.4]** Run `dotnet test Backoffice/Guts.Business.Tests/Guts.Business.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-2, AC-5, AC-8, AC-9, DR-4]` Update `ProjectTeamAssessmentConfiguration.cs` to map `ProjectTeamAssessment.Peers` (`_peers`) via the `ProjectTeamAssessmentPeers` join table, add an EF Core migration, and include a data migration step in `Up(...)` that populates `ProjectTeamAssessmentPeers` for all existing `ProjectTeamAssessments` (from `ProjectTeamUsers` and stored `PeerAssessments`).
- [x] **[T3.2]** `[AC-5, AC-8, AC-9, DR-10]` Write failing repository and migration tests in `Guts.Infrastructure.Tests/Repositories/ProjectTeamDbRepositoryTests.cs`, `ProjectTeamAssessmentDbRepositoryTests.cs`, and `Guts.Infrastructure.Tests/Migrations/` covering:
  - Migration `Up(...)` backfilling `ProjectTeamAssessmentPeers` for existing `ProjectTeamAssessments` and `PeerAssessments` without duplicates.
  - `AddUserToTeam` automatically adding the user to `Peers` of existing incomplete `ProjectTeamAssessment` instances where `DateTime.UtcNow <= DeadlineUtc` (and not adding to completed assessments or assessments past `DeadlineUtc`).
  - `RemoveUserFromTeam` removing the user from `Peers` and deleting their `PeerAssessment` entries in incomplete `ProjectTeamAssessment` instances while preserving `Peers` and `PeerAssessment` entries in completed `ProjectTeamAssessment` instances.
  - `ProjectTeamAssessmentDbRepository.LoadAsync` loading `Peers` and `PeerAssessments.Subject`, and `UpdateAsync` deleting removed `PeerAssessment` entities when a peer is excluded via `SetPeerInclusion(userId, false)`.
- [x] **[T3.3]** `[AC-5, AC-8, AC-9, DR-10]` Implement updates in `ProjectTeamDbRepository.cs` and `ProjectTeamAssessmentDbRepository.cs`.
- [x] **[T3.4]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-9]` Write failing unit tests in `Guts.Api.Tests/Controllers/ProjectTeamAssessmentControllerTests.cs` covering `PUT of-project-assessment/{projectAssessmentId}/of-team/{teamId}/peers/{userId}/inclusion` calling `SetPeerInclusionAsync` and returning `OkResult`.
- [x] **[T4.2]** `[AC-9]` Implement `ProjectTeamAssessmentController.SetPeerInclusion` in `Guts.Api/Controllers/ProjectTeamAssessmentController.cs`.
- [x] **[T4.3]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [ ] **[T5.1]** `[AC-8, AC-9]` Write failing Jasmine unit tests (`*.spec.ts`) in `project.teamassessment.service.spec.ts` and `project-team-edit.component.spec.ts` covering:
  - `ProjectTeamAssessmentService.setPeerInclusion(...)` sending `PUT` request.
  - `ProjectTeamEditComponent` displaying inclusion checkboxes per incomplete open assessment for each team member and invoking `setPeerInclusion` when toggled.
- [ ] **[T5.2]** `[AC-8, AC-9]` Implement updates in `projectassessment.model.ts`, `project.teamassessment.service.ts`, `project-team.component.ts/.html`, and `project-team-edit.component.ts/.html`.
- [ ] **[T5.3]** Run `npm test -- --watch=false` in `Backoffice/Guts.Web/ClientApp` and verify all unit tests pass.
- [ ] **[T5.4]** `[AC-5, AC-6, AC-8, AC-9, DR-11]` Write Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/` covering:
  - Student leaving a team and joining another team (`DR-11`, `AC-8`, `AC-9`).
  - Removing a member from a team with an incomplete vs. completed peer assessment (`AC-5`, `AC-6`, `AC-8`).
  - Teacher toggling a team member's peer inclusion in the Edit Team modal (`AC-9`).
- [ ] **[T5.5]** Run Playwright E2E tests (`npm run test:e2e` in `Backoffice/Guts.Web/ClientApp`) and verify all E2E scenarios pass.

---

## Phase 6: Final Verification & Quality Gate

- [ ] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [ ] **[T6.2]** Run frontend production build & test check: `npm run build`
- [ ] **[T6.3]** Run SonarQube analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and resolve any reported bugs, code smells, or coverage gaps.
- [ ] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
