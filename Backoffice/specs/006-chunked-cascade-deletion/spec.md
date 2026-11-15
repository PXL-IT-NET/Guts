# Specification: Chunked Leaf-to-Root Course & Period Deletion with UI Progress Bar

- **Feature ID**: `006-chunked-cascade-deletion`
- **Status**: `Implemented`
- **Created**: `2026-11-15`

---

## 1. Context & Problem Statement

Deleting a `Course` or `Period` in GUTS removes a large hierarchy of dependent data (`TestResults`, `SolutionFiles`, `TestRuns`, `Tests`, `TestCodeHashes`, `PeerAssessments`, `ProjectTeamAssessments`, `ProjectAssessments`, `ProjectTeamUsers`, `ProjectTeams`, `Assignments`, `Topics`, `GamificationEvents`, `StudentBadges`, `StudentCourseProfiles`, `CourseClassGroups`, and `CourseGamificationConfigs`).

Currently, `CourseDbRepository.DeleteAsync` and `PeriodDbRepository.DeleteAsync` execute a single `ExecuteDeleteAsync()` on `Courses` or `Periods`, triggering SQL Server `ON DELETE CASCADE` inside one large transaction. When thousands of rows are affected, SQL Server escalates locks to exclusive table locks, causing concurrent requests across the rest of the application (such as `GET /api/courses`) to hang and fail while the admin UI waits indefinitely on the single HTTP `DELETE` request. Enabling database-level `READ_COMMITTED_SNAPSHOT` is not viable in environments where the application SQL login does not hold `ALTER DATABASE` permissions.

This feature replaces monolithic cascade deletion with **asynchronous, persistent, chunked leaf-to-root deletion** executed by a background worker in small transactions (`~1,000` rows per batch) using only normal table DDL/DML permissions, paired with a **live progress bar modal** in the Admin Courses and Admin Periods UI.

---

## 2. User Stories & Actors

- **Actors**: `Administrator | Lector (Teacher) | Student | IDE Test Client`

- **US-1**: As an **Administrator**, when I confirm deleting a `Course` or `Period`, I want the server to perform the deletion in small leaf-to-root batches in the background so that students and lectors can continue using GUTS without database lock failures.
- **US-2**: As an **Administrator**, while a `Course` or `Period` deletion is running, I want the confirmation dialog to stay open and locked with a live progress bar showing percentage and current cleanup step until completion, and automatically resume progress display if I reload the page.
- **US-3**: As a **Student** or **Lector**, once a `Course` or `Period` is queued for deletion, I want it to disappear immediately from active course/period lists so I never see partially deleted data.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Persistent `DeletionJob` & Soft-Hide Flag
- **Given** the database schema is migrated for this feature using standard EF Core DDL (`CREATE TABLE`, `ALTER TABLE ... ADD`)
- **When** the migration runs on SQL Server or SQLite
- **Then**:
  - `Courses` and `Periods` each store a non-nullable `IsDeleting` flag (`bool`, default `false`).
  - A `DeletionJobs` table stores persistent deletion job state: `Id` (`Guid`), `TargetType` (`Course` | `Period`), `TargetId` (`int`), `TargetLabel` (`string`), `Status` (`Pending` | `InProgress` | `Completed` | `Failed`), `TotalRows` (`long`), `DeletedRows` (`long`), `CurrentPhase` (`string`), `ErrorMessage` (`string?`), `CreatedAtUtc` (`DateTime`), `StartedAtUtc` (`DateTime?`), `CompletedAtUtc` (`DateTime?`).

### AC-2: Non-Blocking `202 Accepted` Initiation Endpoints
- **Given** an authenticated `Administrator` calls `DELETE /api/courses/{courseId}` or `DELETE /api/periods/{id}` for a deletable entity (`Period.EnsureCanBeDeleted()` passes and `!IsDeleting`)
- **When** the request is processed
- **Then** the backend in a short transaction:
  - Marks `IsDeleting = true` on the target `Course` or `Period`
  - Creates a `DeletionJob` in `Pending` status
  - Notifies the background deletion worker
  - Returns **`202 Accepted`** with a `DeletionJobStatusModel` payload (`jobId`, `targetType`, `targetId`, `targetLabel`, `status`, `totalRows`, `deletedRows`, `percentage`, `currentPhase`, `errorMessage`, timestamps)
- **And** subsequent normal read queries (`GET /api/courses`, `GET /api/courses/{id}`, `GET /api/periods`, `GET /api/periods/{id}`, etc.) immediately exclude entities with `IsDeleting == true`.

### AC-3: Chunked Leaf-to-Root Background Deletion
- **Given** a `Pending` (or interrupted `InProgress` after app restart) `DeletionJob`
- **When** `DeletionBackgroundService` processes the job
- **Then**:
  - Before starting batches (or when starting/resuming), the worker transitions the job to `InProgress`, records `StartedAtUtc`, and computes `TotalRows` across all dependent leaf-to-root tables plus the root row (`>= 1`).
  - Dependent rows are deleted **leaf-to-root** in batches of at most `BatchSize` rows (default `1000`, configurable), each batch committed in its own short transaction so SQL Server row locks are released frequently and stay below table-lock escalation thresholds:
    1. `TestResults` (via target `Assignments`)
    2. `SolutionFiles` (via target `Assignments`)
    3. `PeerAssessments` (via target `ProjectTeamAssessments`)
    4. `ProjectTeamAssessmentPeers` (via target `ProjectTeamAssessments`)
    5. `GamificationEvents` (via target `StudentCourseProfiles`)
    6. `StudentBadges` (via target `StudentCourseProfiles`)
    7. `TestRuns` (via target `Assignments`)
    8. `Tests` (via target `Assignments`)
    9. `TestCodeHashes` (via target `Assignments`)
    10. `ProjectTeamAssessments` (via target `ProjectAssessments`)
    11. `ProjectAssessments` (via target `Topics`)
    12. `ProjectTeamUsers` (via target `ProjectTeams`)
    13. `ProjectTeams` (via target `Topics`)
    14. `Assignments` (via target `Topics`)
    15. `StudentCourseProfiles` (by `CourseId` or `PeriodId`)
    16. `CourseClassGroups` (by `CourseId` or `PeriodId`)
    17. `Topics` (by `CourseId` or `PeriodId`)
    18. `CourseGamificationConfigs` (when `TargetType == Course`)
    19. Target root row (`Course` or `Period`)
  - After every batch, `DeletedRows` (clamped `<= TotalRows`) and `CurrentPhase` (human-readable step description) are persisted to `DeletionJobs`.
  - Once the root row is removed, the job transitions to `Completed` with `DeletedRows == TotalRows`, `percentage = 100`, and `CompletedAtUtc`.
  - If an unexpected error occurs, the job transitions to `Failed` with `ErrorMessage` recorded so the UI and logs report the failure without crashing the host.

### AC-4: Job Status Polling & Active Job Recovery Endpoints
- **Given** an authenticated `Administrator`
- **When** the client calls:
  - `GET /api/deletion-jobs/{jobId}` -> returns `200 OK` with `DeletionJobStatusModel` (or `404 Not Found` if unknown)
  - `GET /api/deletion-jobs/active?targetType={Course|Period}` -> returns `200 OK` with the active (`Pending` or `InProgress`) jobs for that target type (or all active jobs if `targetType` is omitted)
- **Then** the UI can track progress to completion or resume tracking after a page reload.

### AC-5: Locked Confirmation Modal with Live Progress Bar & Reload Recovery
- **Given** an `Administrator` opens `/admin/courses` or `/admin/periods` and confirms deletion in `<app-danger-delete-modal>`
- **When** `DELETE /api/courses/{id}` or `DELETE /api/periods/{id}` returns `202 Accepted`
- **Then**:
  - The `<app-danger-delete-modal>` remains **open and locked** (Close `×`, Cancel, Confirm, and `Escape` disabled)
  - Instead of showing only an opaque spinner, the modal displays a **Bootstrap progress bar** (`0%`–`100%`), numeric counts / percentage, and the active cleanup phase label (e.g. `"Deleting test results (2,000 / 12,450)"`)
  - The Angular component polls `GET /api/deletion-jobs/{jobId}` (e.g. every `500ms`) until `status` is `Completed` or `Failed`
  - On `Completed`: shows a success toast, closes the modal, and refreshes the courses/periods list
  - On `Failed`: shows an error toast with `errorMessage` and unlocks/closes cleanly
  - If the Administrator reloads `/admin/courses` or `/admin/periods` while a deletion job for that resource type is `Pending` or `InProgress`, the component queries `GET /api/deletion-jobs/active`, opens an active-deletion progress modal (or resumes the locked progress modal/banner) and polls until completion.

---

## 4. Domain Rules & Invariants

- **DR-1**: `Course.MarkAsDeleting()` enforces `Contracts.Require(!IsDeleting, ...)` and sets `IsDeleting = true`.
- **DR-2**: `Period.MarkAsDeleting()` calls `EnsureCanBeDeleted()` (`Contracts.Require(!IsActive, "An active period cannot be deleted.")`) and enforces `Contracts.Require(!IsDeleting, ...)`, then sets `IsDeleting = true`.
- **DR-3**: `DeletionJob` (`AggregateRoot` with `Guid Id`) enforces valid state invariants via `Contracts.Require(...)`:
  - `TargetId > 0`, non-empty `TargetLabel`, valid `TargetType`
  - `Start(long totalRows, DateTime utcNow)` sets `TotalRows >= 1`, `Status = InProgress`, `StartedAtUtc = utcNow`
  - `RecordBatchProgress(long rowsDeletedInBatch, string currentPhase)` only allowed when `InProgress`; increments `DeletedRows` safely (non-negative delta, capped cleanly) and updates `CurrentPhase`
  - `Complete(DateTime utcNow)` transitions `InProgress -> Completed`, sets `DeletedRows = TotalRows`, `CurrentPhase = "Completed"`, `CompletedAtUtc = utcNow`
  - `Fail(string errorMessage, DateTime utcNow)` transitions `Pending | InProgress -> Failed`, requires non-empty `errorMessage`, sets `CompletedAtUtc = utcNow`
  - Computed `Percentage` is `0` when `Completed == false && TotalRows <= 0`, `100` when `Completed`, and clamped `[0, 99]` while `InProgress` until the root row is finally deleted (`100`).
- **DR-4**: A `Course` or `Period` with `IsDeleting == true` cannot be updated or deleted a second time (`DataNotFoundException` or `ContractException`).

---

## 5. Authorization & Security Rules

- **Administrators**: Only users satisfying `ApiConstants.AdminOnlyPolicy` may initiate `DELETE /api/courses/{courseId}`, `DELETE /api/periods/{id}`, or query `GET /api/deletion-jobs/*`.
- **Students & Lectors**: Receive `403 Forbidden` if calling deletion or job-status endpoints; normal read endpoints exclude `IsDeleting == true` courses/periods.
- **Unauthenticated**: Receive `401 Unauthorized`.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Invalid `courseId` / `periodId` (`< 1`) or missing `jobId` | Bad request / Not found | `400 Bad Request` / `404 Not Found` |
| Target `Course` or `Period` not found or already `IsDeleting` | Throws `DataNotFoundException` (or `ContractException` if duplicate start) | `404 Not Found` / `400 Bad Request` |
| Attempt to delete active `Period` | Throws `ContractException` from `Period.MarkAsDeleting()` | `400 Bad Request` |
| App host restarts mid-deletion | Startup scan in `DeletionBackgroundService` picks up `Pending`/`InProgress` jobs and finishes remaining leaf-to-root chunks | N/A (background recovery) |
| Empty course or period (0 child rows) | `TotalRows = 1` (root row), deletes root in 1 chunk and reaches `100%` | `202 Accepted` -> `Completed` |
| Failure during background chunk execution | Job marked `Failed` with error details; polling client displays error toast | `200 OK` on status poll (`status = Failed`) |

---

## 7. Non-Functional & Quality Gate Requirements

- **Test Coverage**: Unit and/or integration tests must cover as many lines and branches of all new and modified code as possible across every touched layer (`.NET` domain, application, infrastructure, API controllers/models/background services, and Angular components, services, and utilities), satisfying the SonarCloud new-code coverage Quality Gate (`>= 80%`, targeting maximum achievable coverage).
- **Playwright E2E Coverage**: New or updated UI flows (`AC-5`) for Course and Period deletion progress bar modal, completion refresh, failure handling, and active-job reload recovery must have Playwright E2E test scenarios in `Backoffice/Guts.Web/ClientApp/e2e/tests/`.
- **Static Analysis & Security**: SonarCloud / SonarAnalyzer checks must pass with `0` open issues, `0` unreviewed security hotspots, and `0` ESLint errors.

---

## 8. Out of Scope

- Database-level `ALTER DATABASE ... READ_COMMITTED_SNAPSHOT ON` changes (not permitted by least-privilege SQL Server logins).
- Server-Sent Events (SSE) or SignalR WebSocket push infrastructure.
- Deleting a `Course` by recreating a new `Course` with the exact same `Code` while the old course's background deletion is still running (deferred until root cleanup finishes).

---

## 9. Open Questions

- [x] **Q1**: Orchestration mechanism for chunked deletion + progress updates? — *Decision: Background deletion job (`202 Accepted`) + `BackgroundService` + status polling endpoints.*
- [x] **Q2**: Progress bar UI presentation? — *Decision: Keep `<app-danger-delete-modal>` open and locked with a live progress bar + current cleanup step until `Completed`/`Failed`; on page reload, detect active job(s) and resume polling/progress modal.*
