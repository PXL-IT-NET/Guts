---
name: backoffice-sdd
description: Spec-Driven Development (SDD) workflow for the GUTS Backoffice (.NET DDD backend + Angular SPA). Activate this skill whenever creating, reviewing, designing, breaking down tasks, or implementing a feature using specs in Backoffice/specs/.
---

# GUTS Backoffice — Spec-Driven Development (SDD) Skill

Use this skill to guide features in `Backoffice/` through the 4-phase Spec-Driven Development workflow located in [`Backoffice/specs/`](../../../Backoffice/specs/README.md).

## Templates & Locations

- **Specs Directory**: `Backoffice/specs/`
- **Feature Folder Pattern**: `Backoffice/specs/NNN-feature-slug/` (e.g., `001-exam-export`)
- **Templates**:
  - `Backoffice/specs/_templates/spec.md` — Requirements, User Stories, Acceptance Criteria (`AC-*`), Domain Rules (`DR-*`).
  - `Backoffice/specs/_templates/design.md` — Layered DDD design (`Guts.Domain`, `Guts.Business`, `Guts.Infrastructure`, `Guts.Api`, `Guts.Web/ClientApp`).
  - `Backoffice/specs/_templates/tasks.md` — Inside-out TDD task breakdown with traceability to `AC-*` and `DR-*`.

---

## Phase-by-Phase Instructions

### Phase 1: Specify (`spec.md`)
1. Check existing folders in `Backoffice/specs/` to determine the next `NNN` number.
2. Read `Backoffice/specs/_templates/spec.md`.
3. Explore relevant existing domain aggregates in `Backoffice/Guts.Domain/` and controllers in `Backoffice/Guts.Api/` so the specification uses accurate GUTS domain terminology (`PeriodAggregate`, `CourseAggregate`, `Chapter`, `Project`, `Assignment`, `TestAggregate`, `TestRunAggregate`, `ProjectTeamAggregate`, `ProjectTeamAssessmentAggregate`, `ExamAggregate`).
4. If any requirements, permissions, or edge cases are ambiguous, ask the user clarifying questions before finalizing `spec.md`.
5. Pause and ask the user to review `spec.md` before moving to Phase 2.

### Phase 2: Technical Design (`design.md`)
1. Read the approved `spec.md` and `Backoffice/specs/_templates/design.md`.
2. Inspect the affected files in `Guts.Domain`, `Guts.Business`, `Guts.Infrastructure`, `Guts.Api`, and `Guts.Web/ClientApp`.
3. Ensure the design adheres strictly to [`Backoffice/AGENTS.md`](../../../Backoffice/AGENTS.md):
   - All business rules and state transitions belong in `Guts.Domain` (enforced via `Contracts.Require(...)` from `Guts.Common`).
   - Object creation uses `I*Factory` where appropriate.
   - Naming conventions match `Guts.Bootstrapper/StartUpExtensions.cs` for automatic DI registration.
4. Write `design.md` in the feature's spec directory and pause for user review before generating `tasks.md`.

### Phase 3: Task Breakdown (`tasks.md`)
1. Read `spec.md`, `design.md`, and `Backoffice/specs/_templates/tasks.md`.
2. Create an ordered, inside-out **TDD checklist** in `tasks.md`:
   - Phase 1: `Guts.Domain.Tests` ➔ `Guts.Domain`
   - Phase 2: `Guts.Business.Tests` ➔ `Guts.Business`
   - Phase 3: `Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure` (including EF Core migrations if needed)
   - Phase 4: `Guts.Api.Tests` ➔ `Guts.Api`
   - Phase 5: `Guts.Web/ClientApp` (`*.spec.ts` ➔ Angular components/services)
   - Phase 6: Full test suite & SonarQube verification
3. Tag each task with the corresponding `[AC-*]` / `[DR-*]` identifiers.

### Phase 4: TDD Execution
1. Work through `tasks.md` sequentially (either one phase at a time or one task at a time as requested by the user).
2. For each task:
   - Write the failing unit test first (reusing `Builders/` and `RandomExtensions`).
   - Implement the production code to make the test pass.
   - Run the targeted test project (`dotnet test ...` or `npm test -- --watch=false`).
   - Mark the task as `[x]` in `tasks.md`.
3. Once all tasks are complete, run the full verification suite (`dotnet test Backoffice/Guts.slnx`) and update the status in `spec.md` and `design.md` to `Implemented`.

