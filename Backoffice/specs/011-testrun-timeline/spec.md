# Specification: Interactive Test Run Timeline (Time-Travel Inspection)

- **Feature ID**: `011-testrun-timeline`
- **Status**: `Implemented`
- **Created**: `2026-11-20`

---

## 1. Context & Problem Statement

On the assignment test-results detail view (`AssignmentDetailComponent`, shared by both Chapter exercises and Project components), users currently see only a static snapshot of the latest `TestResults` and `SolutionFiles` as of the page's `statusDate` (along with first/last run timestamps and total run count). Inspecting how a student or project team progressed over time—when tests turned green or regressed, whether they modified source code between runs, or what their code looked like at an earlier milestone—requires guessing timestamps manually.

This feature adds an **interactive Test Run Timeline** to `AssignmentDetailComponent`:
- Loads **asynchronously after** the initial latest assignment detail has finished rendering (displaying a compact loading indicator in the meantime so initial page load stays fast).
- Renders a progression graph of cumulative passing tests over time where **each dot represents a `TestRun`**, visually distinguishing runs where **test results changed**, runs where **only source code changed**, and runs where **nothing changed**.
- Lets both **Lectors** and **Students** (respecting existing assignment authorization) drag a vertical beam across the timeline—or click dots / use `◀ Prev` / `Next ▶` / keyboard arrow keys—to travel back in time and inspect the exact `TestResults` and `SolutionFiles` at any historical test run while preserving expanded source-code file panels.

---

## 2. User Stories & Actors

- **Actors**: `Lector (Teacher)`, `Student`

- **US-1 (Asynchronous Timeline Loading)**: As a **Lector** or **Student**, I want the assignment detail page to load and display the latest test results and source code immediately first, and then load the test-run timeline asynchronously with a clear loading state, so the page feels fast and responsive even for assignments with many runs.
- **US-2 (Visual Progression & Change Categorization)**: As a **Lector** or **Student**, I want to see a graph of cumulative passing tests where each dot is a test run, with clear visual distinction between runs where test results changed, runs where only solution files changed, and runs where nothing changed, so I can spot meaningful milestones at a glance.
- **US-3 (Interactive Time-Travel Scrubbing)**: As a **Lector** or **Student**, I want to drag a vertical beam across the timeline (or click a dot / use navigation buttons or keyboard arrows) to snap to any historical test run and view the exact `TestResults` and `SolutionFiles` at that point in time, with expanded source-code files staying open as I scrub and a one-click "Back to latest" button.
- **US-4 (Flexible X-Axis Scale)**: As a **Lector**, I want the timeline X-axis to default to evenly spaced runs (so dense bursts of runs are easy to drag across) with a toggle to switch to a proportional **Real-Time** scale (so idle gaps and bursts of activity are immediately visible).

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Two-Stage Asynchronous Loading on `AssignmentDetailComponent`
- **Given** a user opens an assignment detail view (`assignmentId > 0` and either `userId > 0` or `teamId > 0`) with an effective upper-bound `statusDate`
- **When** the component loads
- **Then**:
  1. It first fetches and renders the latest assignment detail (`getAssignmentDetail(assignmentId, userId, teamId, statusDate)`) as today.
  2. Immediately after the initial detail response succeeds (and only if `numberOfRuns > 1`), it initiates an asynchronous request to fetch the timeline data capped at `statusDate`, displaying a compact loading state (spinner + label) in the timeline slot above **Source code** and **Test results**.
  3. Once the timeline response arrives:
     - If `runs.length > 1`, the interactive timeline card replaces the loading state with the beam positioned at the latest run.
     - If `runs.length <= 1`, the timeline card is hidden.
     - If the timeline request fails, the error is handled gracefully (subtle non-blocking warning without breaking the already-loaded latest test results and source code).
  4. When `@Input()`s (`assignmentId`, `userId`, `teamId`, or `statusDate`) change, any in-flight timeline/snapshot request is cancelled (`switchMap` / subscription teardown), the snapshot cache is cleared, and the two-stage load restarts cleanly.

### AC-2: Timeline Data Computation & Change Categorization (Backend Domain Policy)
- **Given** all `TestRun`s (and their `TestResult`s) and `SolutionFile` modification timestamps for `(assignmentId, userId)` (Chapters) or `(assignmentId, teamId)` (Projects) up to `dateUtc` (inclusive), ordered strictly by `(CreateDateTime ASC, TestRunId ASC)`
- **When** `TestRunTimelineBuilder` (pure domain service in `Guts.Domain`) builds the timeline entries
- **Then** for each `TestRun` (1-based `runNumber`):
  - Maintains cumulative pass/fail state (`TestId -> Passed`) across all prior runs so partial single-test runs accurately compute `passedTestsCount` (`0..totalTestsCount`) and `failedTestsCount`.
  - Sets `testResultsChanged = true` if at least one `TestResult` in the run introduces a new `TestId` or flips `Passed` (`true <-> false`) relative to the cumulative state (or if it is the very first run with results); otherwise `false`.
  - Sets `testResultsDelta` (`passedTestsCount - previousPassedTestsCount`, or `0` on the first run) so the UI can distinguish improvement (`> 0`), regression (`< 0`), and first/neutral change (`== 0`).
  - Sets `solutionFilesChanged = true` if at least one `SolutionFile` for that `(Assignment, User/Team)` was saved during that run's window—specifically matching `SolutionFile.ModifyDateTime` against `(previousRunTime + tolerance, currentRunTime + tolerance]` (for run #1: `<= currentRunTime + tolerance`, where `tolerance = 2 seconds` accounts for `SolutionFile.ModifyDateTime` being recorded immediately before `TestRun.CreateDateTime` in `TestRunService.RegisterRunAsync`); otherwise `false`.
  - Records `testRunId`, `createDateTimeUtc`, `passedTestsCount`, `totalTestsCount`, `testResultsChanged`, `solutionFilesChanged`, and `testResultsDelta` (plus `userId` / `userName` for team runs if helpful in tooltip).

### AC-3: Timeline Graph & 3-Tier Dot Visualization
- **Given** a loaded timeline with `runs.length > 1`
- **When** the timeline component renders above **Source code** and **Test results**
- **Then**:
  - Plots an interactive **Chart.js** line progression graph (using the existing `chart.js` / `ng2-charts` `baseChart` setup) with **Y-axis = cumulative passing tests** (`0` to `totalTestsCount`) and a connecting line/area curve.
  - Renders **one dot per `TestRun`** using **3 distinct visual tiers** accompanied by a concise legend:
    1. **Test results changed** (`testResultsChanged == true`): **Large solid dot** — green when improved (`testResultsDelta > 0`) or 100% passing, red/danger when regressed (`testResultsDelta < 0`), primary/blue on first run or neutral change (`testResultsDelta == 0`).
    2. **Source code changed only** (`!testResultsChanged && solutionFilesChanged`): **Medium hollow/ring dot** (distinct accent border with light fill).
    3. **No change** (`!testResultsChanged && !solutionFilesChanged`): **Small muted gray dot**.
  - Hovering over any dot (or dragging the beam across it) displays a clear tooltip/readout showing: Run `#N of Total`, local formatted timestamp, `passed / total` tests, and exact change status (*"Test results & source code changed"*, *"Test results changed"*, *"Source code changed"*, or *"No changes"*).

### AC-4: X-Axis Scale Toggle (Evenly Spaced vs. Real-Time)
- **Given** a rendered timeline
- **When** the user views the timeline
- **Then**:
  - The X-axis defaults to **Evenly spaced** runs (`Sequence` mode) so dense bursts of runs remain easy to drag across and click, displaying formatted timestamps on axis ticks and the active readout.
  - A toggle button (`Sequence` / `Real-time`) switches the X-axis to a **proportional Real-Time scale** (`firstRunTime..lastRunTime`) so idle gaps and bursts of activity are visually proportional.
  - Switching scale preserves the currently selected run and beam position.

### AC-5: Interactive Draggable Beam, Point-in-Time Inspection & Caching
- **Given** a rendered timeline
- **When** the user interacts with the timeline by:
  - Dragging the vertical beam (pointer/mouse/touch drag across the Chart.js canvas), OR
  - Clicking directly on any dot (or clicking anywhere on the plot area), OR
  - Clicking `◀ Prev` / `Next ▶` buttons, OR
  - Pressing `ArrowLeft` / `ArrowRight` / `Home` / `End` keys while the timeline beam/slider is focused
- **Then**:
  - **During drag**: the vertical beam snaps smoothly to the nearest test-run dot and updates the live header/tooltip preview immediately **without** firing HTTP requests on every pixel move.
  - **On release / click / button / keypress**: if the selected run differs from the currently displayed snapshot:
    - Checks an in-memory cache (keyed by `testRunId`) first; if cached, displays it immediately with zero HTTP calls.
    - Otherwise fetches the historical snapshot via the existing point-in-time `getAssignmentDetail(assignmentId, userId, teamId, run.createDateTimeUtc)` endpoint (showing a non-jarring loading overlay on the detail section while fetching) and caches the result.
    - **Preserves expanded source-code file panels**: any `SolutionFile` whose `filePath` was expanded before switching snapshots remains expanded in the newly loaded snapshot (if that file exists at the target point in time).
  - When viewing any run earlier than the latest run:
    - Displays a clear contextual banner (e.g., *"Viewing historical snapshot: Run #X of N — [timestamp]"*) with a **"Back to latest"** button that snaps the beam back to the latest run and restores the latest snapshot immediately from cache.
  - Scrubbing the timeline stays local to `AssignmentDetailComponent` and does **not** alter the parent Chapter/Project page's `statusDate` filter.

### AC-6: Timeline REST API Endpoints & Authorization
- **Given** `AssignmentController` (`Guts.Api/Controllers/AssignmentController.cs`)
- **When** requesting timeline data:
  - `GET /api/assignments/{assignmentId}/foruser/{userId}/timeline?date={optionalDate}`:
    - Same authorization as `GetAssignmentResultsForUser`: Students may only access their own `userId` (`403 Forbidden` otherwise); Lectors may access any `userId`.
  - `GET /api/assignments/{assignmentId}/forteam/{teamId}/timeline?date={optionalDate}`:
    - Same authorization as `GetAssignmentResultsForTeam`: Students may only access teams they belong to (`403 Forbidden` otherwise); Lectors may access any team.
- **Then** returns `200 OK` with `AssignmentTimelineModel`:
  - `assignmentId`, `totalTestsCount`, and ordered `runs` (`AssignmentTimelinePointModel[]`).

---

## 4. Domain Rules & Invariants

All timeline progression & change-detection logic must reside in `Guts.Domain` (zero business logic in `Guts.Application` or `Guts.Api`):

- **DR-1 (Chronological Ordering)**: Timeline entries are ordered strictly by `(CreateDateTime ASC, TestRunId ASC)` and assigned 1-based sequential `RunNumber`s.
- **DR-2 (Cumulative Pass/Fail Progression)**: Each run's `PassedTestsCount`, `FailedTestsCount`, `TestResultsChanged`, and `TestResultsDelta` are computed over the running cumulative `TestId -> Passed` map so partial single-test runs reflect true cumulative progress (never falsely dropping untested tests to 0).
- **DR-3 (Solution File Correlation)**: Each `SolutionFile.ModifyDateTime` (up to `dateUtc`, if provided) is matched to the earliest `TestRun` whose `CreateDateTime + CorrelationTolerance` (`2 seconds`) is `>= ModifyDateTime` (marking that run `SolutionFilesChanged = true`), ensuring files saved right before `TestRun` insertion in `TestRunService.RegisterRunAsync` are reliably attributed to the exact run that triggered them, without ever double-counting a file across runs.
- **DR-4 (Point-in-Time Consistency)**: When `dateUtc` is supplied, only `TestRun`s with `CreateDateTime <= dateUtc` and `SolutionFile`s with `ModifyDateTime <= dateUtc` are included.

---

## 5. Authorization & Security Rules

- **Students**: May fetch the timeline only for their own `userId` (Chapters) or a `ProjectTeam` they are a member of (Projects); attempting to query another student or team returns `403 Forbidden`.
- **Lectors**: May fetch the timeline for any student or project team.
- **Unauthenticated**: Rejected with `401 Unauthorized`.
- **Impersonation**: Read-only `GET` endpoints work normally during impersonation (consistent with `GetAssignmentResultsForUser` / `GetAssignmentResultsForTeam`).

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Student requests timeline for another student or non-member team | Returns `Forbid()` | `403 Forbidden` |
| Assignment or team does not exist | Throws `DataNotFoundException` ➔ handled by `ApplicationExceptionFilterAttribute` | `404 Not Found` |
| Assignment has `0` or `1` test runs | Timeline endpoint returns `0` or `1` item; UI hides timeline card cleanly | `200 OK` |
| All test runs have identical timestamps (rare edge case in Real-Time scale) | Real-Time scale falls back gracefully to evenly distributed X positions (no division by zero / `NaN` chart coordinates) | `200 OK` |
| Assignment has `0` tests (`totalTestsCount == 0`) | Y-axis renders safely with baseline `0..1` scale (no division by zero) | `200 OK` |
| Student switches to another assignment/student while timeline or snapshot is loading | Previous observable is unsubscribed; stale response cannot overwrite the newly selected assignment | N/A (Frontend) |
| Timeline HTTP request fails (e.g., 500) | Loading spinner clears; non-blocking warning shown; latest test results & source code remain fully usable | `500` handled gracefully |
| Two runs have identical timestamps (within milliseconds) | Point-in-time query uses run's exact UTC ISO timestamp (with millisecond precision) | `200 OK` |

---

## 7. Non-Functional & Quality Gate Requirements

- **Performance**: Timeline backend query must use lightweight `AsNoTracking()` projections (`TestRunId`, `CreateDateTime`, `TestId`, `Passed`, and `SolutionFile.ModifyDateTime` ONLY—never loading `SolutionFile.Content` or `TestResult.Message` blobs!).
- **Test Coverage**: Unit and integration tests covering as many lines and branches as possible across `Guts.Domain.Tests` (`TestRunTimelineBuilderTests`), `Guts.Application.Tests`, `Guts.Infrastructure.Tests`, `Guts.Api.Tests` (`AssignmentControllerTests`), and Angular Karma/Jasmine specs (`assignment-timeline.component.spec.ts`, `assignmentdetail.component.spec.ts`, `assignment.service.spec.ts`), passing SonarCloud new-code coverage (`>= 80%`, targeting ~100%).
- **Playwright E2E Coverage**: New Playwright E2E spec in `Backoffice/Guts.Web/ClientApp/e2e/tests/` verifying asynchronous timeline loading & loading state, 3-tier dot rendering & legend, dragging/clicking the beam to load a historical snapshot, preserving expanded source-code panels across snapshots, "Back to latest" restoration, X-axis scale toggle, and hiding timeline when `<= 1` run.
- **Static Analysis & Security**: SonarCloud / SonarAnalyzer checks must pass with `0` open issues, `0` unreviewed security hotspots, and `0` ESLint errors.

---

## 8. Out of Scope

- Side-by-side text diffing between two historical `SolutionFile` versions (viewing the full code at any selected run is in scope; inline diff highlighting can be a future enhancement).
- Changing or mutating the parent Chapter/Project page's global `statusDate` picker when scrubbing the detail timeline.
- Exporting timeline charts as images/CSV.

---

## 9. Open Questions

- [x] **Q1**: Who can see and use the timeline? — *Decision: Both Lectors and Students (same authorization as `AssignmentDetail`).*
- [x] **Q2**: Graph axes & X-axis scale? — *Decision: Y-axis = cumulative passing tests; X-axis defaults to evenly spaced runs with a toggle to proportional Real-Time scale.*
- [x] **Q3**: Visual distinction of dots? — *Decision: 3 visual tiers + legend (large solid dot for test results changed [green/red/blue], medium hollow ring dot for source code changed only, small muted gray dot for no change) + detailed tooltip.*
- [x] **Q4**: Beam dragging & snapshot loading behavior? — *Decision: Snap beam to nearest dot with live preview while dragging; load `TestResults` & `SolutionFiles` on release/click/Prev-Next/keyboard arrows; cache visited snapshots in memory; show historical banner + "Back to latest" button.*
- [x] **Q5**: Placement, single-run behavior, and source-code panel state? — *Decision: Top of `AssignmentDetail` (above Source code & Test results), compact loading state after initial detail loads, hide when `<= 1` run, and preserve expanded Source code panels per `filePath` across snapshot switches.*
