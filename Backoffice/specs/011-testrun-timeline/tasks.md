# TDD Implementation Plan: Interactive Test Run Timeline (Time-Travel Inspection)

- **Feature ID**: `011-testrun-timeline`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-2, DR-1, DR-2, DR-3]` Write failing zero-mock unit tests in `Backoffice/Guts.Domain.Tests/TestRunAggregate/TestRunTimelineBuilderTests.cs` covering:
  - Null/empty runs returns empty list.
  - Chronological sorting by `(CreateDateTime ASC, TestRunId ASC)` with 1-based sequential `RunNumber`s.
  - Cumulative pass/fail progression across full and partial single-test runs (computing `PassedTestsCount`, `FailedTestsCount`, `TestResultsChanged`, and `TestResultsDelta` for improvements, regressions, neutral/first run, and unchanged runs).
  - `SolutionFile` timestamp correlation with 2-second tolerance (file modified right before run attributes to that run; subsequent runs without new file versions have `SolutionFilesChanged == false`; multiple files in one run window only mark that run once; file timestamp after all runs is ignored safely).
  - Unchanged run (`TestResultsChanged == false && SolutionFilesChanged == false`) vs code-only change (`!TestResultsChanged && SolutionFilesChanged`) vs test-result change (`TestResultsChanged`).
- [x] **[T1.2]** `[AC-2, DR-1, DR-2, DR-3]` Implement `TestRunTimelinePoint` and `TestRunTimelineBuilder` in `Backoffice/Guts.Domain/TestRunAggregate/TestRunTimelineBuilder.cs`.
- [x] **[T1.3]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Application Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-2, AC-6, DR-4]` Add `AssignmentTimelineDto` in `Backoffice/Guts.Application/Dtos/AssignmentTimelineDto.cs`, expand `ITestRunRepository` (`GetUserTestRunSnapshotsForAssignmentAsync`, `GetTeamTestRunSnapshotsForAssignmentAsync`), `ISolutionFileRepository` (`GetModifyDateTimesForUserAsync`, `GetModifyDateTimesForTeamAsync`), and `IAssignmentService` (`GetTimelineForUserAsync`, `GetTimelineForTeamAsync`).
- [x] **[T2.2]** `[AC-2, DR-4]` Write failing unit tests in `Backoffice/Guts.Application.Tests/Services/AssignmentServiceTests.cs` covering `GetTimelineForUserAsync` and `GetTimelineForTeamAsync` (with runs, with `0` runs skipping `SolutionFiles` query, and forwarding `dateUtc`).
- [x] **[T2.3]** `[AC-2, DR-4]` Implement `GetTimelineForUserAsync` and `GetTimelineForTeamAsync` in `Backoffice/Guts.Application/Services/AssignmentService.cs`.
- [x] **[T2.4]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-2, DR-4]` Write failing integration tests in `Backoffice/Guts.Infrastructure.Tests/Repositories/TestRunDbRepositoryTests.cs` and `Backoffice/Guts.Infrastructure.Tests/Repositories/SolutionFileDbRepositoryTests.cs` covering user & team snapshot projections and `ModifyDateTime` projections (including `dateUtc` upper-bound filtering).
- [x] **[T3.2]** `[AC-2, DR-4]` Implement `GetUserTestRunSnapshotsForAssignmentAsync` / `GetTeamTestRunSnapshotsForAssignmentAsync` in `TestRunDbRepository.cs` and `GetModifyDateTimesForUserAsync` / `GetModifyDateTimesForTeamAsync` in `SolutionFileDbRepository.cs` using lean `AsNoTracking()` projections.
- [x] **[T3.3]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-6]` Write failing unit tests in `Backoffice/Guts.Api.Tests/Controllers/AssignmentControllerTests.cs` covering `GET /api/assignments/{id}/foruser/{userId}/timeline` and `GET /api/assignments/{id}/forteam/{teamId}/timeline` (Lector access, Student own/team access, Student forbidden for other student/team, `date` UTC forwarding, and response model mapping).
- [x] **[T4.2]** `[AC-6]` Implement `AssignmentTimelineModel` / `AssignmentTimelinePointModel` in `Backoffice/Guts.Api/Models/AssignmentTimelineModel.cs` and the two timeline actions on `Backoffice/Guts.Api/Controllers/AssignmentController.cs`.
- [x] **[T4.3]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T5.1]** `[AC-3, AC-4]` Create `Backoffice/Guts.Web/ClientApp/src/app/viewmodels/assignment-timeline.model.ts` and write failing Jasmine unit tests (`assignment-timeline.model.spec.ts`) covering `classifyTimelineDot`, `getTimelineChangeSummary`, `getTimelinePointXValue`, and `findNearestRunIndex` (including identical-timestamp fallback).
- [x] **[T5.2]** `[AC-1, AC-6]` Add `getAssignmentTimeline` to `AssignmentService` (`src/app/services/assignment.service.ts`) and add unit tests in `assignment.service.spec.ts` (user URL, team URL, optional date query param, HTTP error handling).
- [x] **[T5.3]** `[AC-3, AC-4, AC-5]` Implement standalone Chart.js `AssignmentTimelineComponent` (`src/app/components/assignment-timeline/` using `BaseChartDirective`, 3-tier dot styling + legend, custom draggable vertical beam + dimmed future overlay plugin, pointer capture drag/click snapping, `Sequence`/`Real-time` scale toggle, `◀ Prev`/`Next ▶` buttons, keyboard slider support) and write comprehensive Jasmine unit tests (`assignment-timeline.component.spec.ts`).
- [x] **[T5.4]** `[AC-1, AC-5]` Import `AssignmentTimelineComponent` in `AppModule`, update `AssignmentDetailComponent` (`assignmentdetail.component.ts` & `.html`) for two-stage async loading (latest detail first ➔ compact loading indicator ➔ timeline card when `> 1` run), remove obsolete First/Last/Number of test runs form inputs, in-memory snapshot caching, expanded `SolutionFile` preservation across snapshots, historical snapshot banner + "Back to latest" button, and non-blocking error state; update `assignmentdetail.component.spec.ts`.
- [x] **[T5.5]** `[AC-1, AC-3, AC-4, AC-5]` Add Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/assignment-timeline.spec.ts` covering:
  - Two-stage async loading (loading state ➔ rendered Chart.js timeline + legend).
  - Clicking/dragging/navigating (`◀ Prev`, `Next ▶`, `Back to latest`, keyboard arrows) to load a historical snapshot, displaying the historical banner, and preserving expanded Source code panels across snapshots.
  - Toggling `Sequence` / `Real-time` X-axis scale.
  - Hiding timeline when `<= 1` run and graceful fallback when timeline request fails.
- [x] **[T5.6]** Run `npm run lint`, `npm test -- --watch=false --browsers=ChromeHeadless`, `npm run build`, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp` and verify all pass.

---

## Phase 6: Final Verification & Quality Gate

- [x] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [x] **[T6.2]** Run frontend lint, Karma unit tests, production build, and Playwright E2E tests.
- [x] **[T6.3]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and inspect per-file new-code coverage; add missing tests for uncovered lines/branches across backend and frontend until Quality Gate is `OK` (`>= 80%`, aiming as close to `100%` as possible, `0` open issues, and `0` unreviewed hotspots).
- [x] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
