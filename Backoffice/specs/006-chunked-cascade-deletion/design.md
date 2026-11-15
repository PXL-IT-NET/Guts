# Technical Design: Chunked Leaf-to-Root Course & Period Deletion with UI Progress Bar

- **Feature ID**: `006-chunked-cascade-deletion`
- **Specification**: [`spec.md`](./spec.md)
- **Status**: `Implemented`

---

## 1. Architectural Overview

Instead of deleting a `Course` or `Period` synchronously via a single cascading SQL `DELETE`, deletion becomes a persistent two-phase workflow:

1. **Immediate Initiation (`DELETE /api/courses/{courseId}` & `DELETE /api/periods/{id}`)**:
   - `CourseService` / `PeriodService` validates domain preconditions (`Course.MarkAsDeleting()` / `Period.MarkAsDeleting()`), persists `IsDeleting = true` alongside a new `DeletionJob` (`Pending`) inside a short transaction, signals `IDeletionJobQueue`, and returns `202 Accepted` with `DeletionJobStatusModel`.
   - Normal read queries immediately filter out entities with `IsDeleting == true`.
2. **Background Chunked Leaf-to-Root Cleanup (`DeletionBackgroundService`)**:
   - Picks up pending (or interrupted `InProgress` on host restart) `DeletionJob` records.
   - Counts total affected rows leaf-to-root, transitions the job to `InProgress`, deletes child rows strictly **leaf-to-root** in small batches (`BatchSize`, default `1000`) inside short independent transactions via `IChunkedDeletionRepository`, and persists `DeletedRows` & `CurrentPhase` after every chunk.
   - Finishes by deleting the `Course` or `Period` root row and marking the job `Completed`.
3. **Angular Live Progress Bar & Recovery**:
   - `DeletionJobService` polls `GET /api/deletion-jobs/{jobId}` and queries `GET /api/deletion-jobs/active?targetType=...`.
   - `DangerDeleteModalComponent` supports locked progress-bar mode (`inProgress`, `progressPercentage`, `progressLabel`) shared by `AdminCoursesComponent` and `AdminPeriodsComponent`.

No `ALTER DATABASE` or `READ_COMMITTED_SNAPSHOT` SQL is used anywhere; only standard EF Core table/column DDL is required.

---

## 2. Domain Layer (`Guts.Domain`)

### Modified / New Types
- **`Course`** (`Guts.Domain/CourseAggregate/Course.cs`):
  - Property: `public bool IsDeleting { get; private set; }`
  - Method: `public void MarkAsDeleting()` — enforces `Contracts.Require(!IsDeleting, "This course is already queued for deletion.");` and sets `IsDeleting = true`.
  - Update guard in `Update(...)`: `Contracts.Require(!IsDeleting, "A course that is being deleted cannot be updated.");`
- **`Period` / `IPeriod`** (`Guts.Domain/PeriodAggregate/`):
  - Property: `public bool IsDeleting { get; private set; }` (exposed on `IPeriod`)
  - Method: `public void MarkAsDeleting()` (exposed on `IPeriod`) — calls `EnsureCanBeDeleted()`, enforces `Contracts.Require(!IsDeleting, "This period is already queued for deletion.");`, sets `IsDeleting = true`.
  - Update guard in `Update(...)`: `Contracts.Require(!IsDeleting, "A period that is being deleted cannot be updated.");`
- **`DeletionTargetType`** (`Guts.Domain/DeletionJobAggregate/DeletionTargetType.cs`):
  - Enum: `Course = 1`, `Period = 2`
- **`DeletionJobStatus`** (`Guts.Domain/DeletionJobAggregate/DeletionJobStatus.cs`):
  - Enum: `Pending = 1`, `InProgress = 2`, `Completed = 3`, `Failed = 4`
- **`DeletionJob`** (`Guts.Domain/DeletionJobAggregate/DeletionJob.cs` implementing `AggregateRoot`):
  - Properties (private setters): `Id` (`Guid`), `TargetType` (`DeletionTargetType`), `TargetId` (`int`), `TargetLabel` (`string`), `Status` (`DeletionJobStatus`), `TotalRows` (`long`), `DeletedRows` (`long`), `CurrentPhase` (`string`), `ErrorMessage` (`string`), `CreatedAtUtc` (`DateTime`), `StartedAtUtc` (`DateTime?`), `CompletedAtUtc` (`DateTime?`).
  - Domain methods:
    - `Start(long totalRows, DateTime utcNow, string initialPhase)`: requires `Status == Pending`, `totalRows >= 1`, `!string.IsNullOrWhiteSpace(initialPhase)`; sets `Status = InProgress`, `TotalRows = totalRows`, `DeletedRows = 0`, `CurrentPhase = initialPhase.Trim()`, `StartedAtUtc = utcNow`.
    - `RecordBatchProgress(long rowsDeletedInBatch, string currentPhase)`: requires `Status == InProgress`, `rowsDeletedInBatch >= 0`, `!string.IsNullOrWhiteSpace(currentPhase)`; increments `DeletedRows = Math.Min(TotalRows, DeletedRows + rowsDeletedInBatch)`, sets `CurrentPhase = currentPhase.Trim()`.
    - `Complete(DateTime utcNow)`: requires `Status == InProgress`; sets `Status = Completed`, `DeletedRows = TotalRows`, `CurrentPhase = "Completed"`, `CompletedAtUtc = utcNow`.
    - `Fail(string errorMessage, DateTime utcNow)`: requires `Status is Pending or InProgress`, `!string.IsNullOrWhiteSpace(errorMessage)`; sets `Status = Failed`, `ErrorMessage = errorMessage.Trim()`, `CompletedAtUtc = utcNow`.
    - `ResetForResume(long remainingTotalRows, DateTime utcNow, string initialPhase)`: used when recovering an interrupted `Pending` or `InProgress` job after a server restart; resets `TotalRows = Math.Max(1, remainingTotalRows)`, `DeletedRows = 0`, `Status = InProgress`, `StartedAtUtc ??= utcNow`, `CurrentPhase = initialPhase.Trim()`.
    - `int CalculatePercentage()`: returns `0` for `Pending`, `100` for `Completed`, or `(int)Math.Clamp((DeletedRows * 100L) / Math.Max(1, TotalRows), 0, 99)` while `InProgress` / `Failed`.
- **Factory**:
  - `IDeletionJobFactory` / `DeletionJob.Factory` (`Guts.Domain/DeletionJobAggregate/IDeletionJobFactory.cs` & nested `Factory` in `DeletionJob.cs` — check `StartUpExtensions.RegisterTypesWhoseNameEndsWith("Factory", ...)`)
  - Actually: nested classes inside `Course`/`Period` work via `ICourseFactory` / `IPeriodFactory` where the nested class is named `Factory`! Let's follow `MessageTrackerFactory` naming (`public class DeletionJobFactory : IDeletionJobFactory`) so `Type.Name.EndsWith("Factory")` automatically discovers and registers `IDeletionJobFactory` -> `DeletionJobFactory` as Singleton in `StartUpExtensions.AddDomainLayerServices()`!

---

## 3. Application Layer (`Guts.Application`) — Use-Case Orchestration Only

> [!IMPORTANT]
> Zero domain logic in `Guts.Application`. Services coordinate repository loading, domain method invocation, persistence, DTO mapping, and background queue signaling.

### Repository Contracts (`Guts.Application/Repositories/`)
- **`IDeletionJobRepository`**:
  - `Task<DeletionJob> GetByIdAsync(Guid jobId)` (throws `DataNotFoundException` if missing)
  - `Task<IReadOnlyList<DeletionJob>> GetActiveAsync(DeletionTargetType? targetType = null)`
  - `Task<DeletionJob> AddAsync(DeletionJob job)`
  - `Task UpdateAsync(DeletionJob job)`
- **`IChunkedDeletionRepository`**:
  - `Task<long> CountDependentAndRootRowsAsync(DeletionTargetType targetType, int targetId, CancellationToken cancellationToken = default)`
  - `Task ExecuteChunkedDeletionAsync(DeletionTargetType targetType, int targetId, int batchSize, Func<int, string, Task> onBatchDeletedAsync, CancellationToken cancellationToken = default)`
- **Updates to `ICourseRepository` / `IPeriodRepository`**:
  - Add `Task QueueDeletionAsync(Course course, DeletionJob job)` / `Task QueueDeletionAsync(IPeriod period, DeletionJob job)` (or use shared transaction helper to persist both `IsDeleting = true` and the `DeletionJob` atomically in a short transaction).

### Queue Signaler Contract (`Guts.Application/Services/Deletion/`)
- **`IDeletionJobQueue`**:
  - `ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default)`
  - `IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken)`

### Application Services & DTOs
- **`DeletionJobDto`** (`Guts.Application/Dtos/DeletionJobDto.cs`) + **`IDeletionJobConverter`** (`Guts.Application/Converters/DeletionJobConverter.cs`)
- **`IDeletionJobService` / `DeletionJobService`** (`Guts.Application/Services/Deletion/`):
  - `Task<DeletionJobDto> GetJobStatusAsync(Guid jobId)`
  - `Task<IReadOnlyList<DeletionJobDto>> GetActiveJobsAsync(DeletionTargetType? targetType = null)`
  - `Task<int> ProcessPendingJobsAsync(int batchSize, CancellationToken cancellationToken = default)`:
    - Loads all `Pending` / `InProgress` jobs ordered by `CreatedAtUtc`
    - For each job: counts remaining rows, calls `job.Start(...)` (or `job.ResetForResume(...)` if already `InProgress`), updates DB, calls `IChunkedDeletionRepository.ExecuteChunkedDeletionAsync(...)` updating `job.RecordBatchProgress(...)` + DB after each batch, then calls `job.Complete(utcNow)` (or `job.Fail(ex.Message, utcNow)` on exception) and persists.
- **Updates to `ICourseService.DeleteCourseAsync` & `IPeriodService.DeletePeriodAsync`**:
  - Signature returns `Task<DeletionJobDto>`
  - Orchestration: load aggregate -> invoke `MarkAsDeleting()` -> create `DeletionJob` via `IDeletionJobFactory` -> persist atomically via `QueueDeletionAsync` -> `await deletionJobQueue.EnqueueAsync(job.Id)` -> return mapped `DeletionJobDto`.

---

## 4. Infrastructure Layer (`Guts.Infrastructure`)

### EF Core Configuration & Migration
- **`GutsContext`**: Add `public DbSet<DeletionJob> DeletionJobs { get; set; }`.
- **`CourseConfiguration`** (or `GutsContext` entity config) & `Period` config: map `IsDeleting` (`bool`, non-nullable, default `false`).
- **`DeletionJobConfiguration`** (`Guts.Infrastructure/EntityConfigurations/DeletionJobConfiguration.cs`):
  - Table `"DeletionJobs"`, PK `Id`, `TargetType` (`int`), `TargetId`, `TargetLabel` (`MaxLength(255)`), `Status` (`int`), `TotalRows`, `DeletedRows`, `CurrentPhase` (`MaxLength(255)`), `ErrorMessage` (`MaxLength(1024)`), UTC timestamps, index on `(Status, TargetType)`.
- **Migration Required**: `Yes` — `AddChunkedDeletionJobs` (adds `Courses.IsDeleting`, `Periods.IsDeleting`, and `DeletionJobs` table; **zero** `ALTER DATABASE` / `READ_COMMITTED_SNAPSHOT` SQL).

### Query Filtering (`CourseDbRepository` & `PeriodDbRepository`)
- Filter `!IsDeleting` in normal reads (`GetAllAsync`, `GetByIdAsync`, `GetSingleAsync`, `GetPeriodAsync`), and add `QueueDeletionAsync` which updates the aggregate and inserts the `DeletionJob` in a single short `ExecuteInTransactionAsync`.

### Leaf-to-Root Chunked Deletion (`ChunkedDeletionDbRepository`)
- Resolves scoped `topicIds`, `assignmentIds`, `projectTeamIds`, `projectAssessmentIds`, `projectTeamAssessmentIds`, `studentCourseProfileIds` cleanly using `.Where(...)` / `.Select(x => x.Id)`.
- Processes parent ID sets in slices of `<= 500` IDs to stay well below SQL Server's 2,100-parameter limit.
- Runs each batch in a short independent transaction (`ExecuteInTransactionAsync` + `.Take(batchSize).ExecuteDeleteAsync()`) across these phases in strict leaf-to-root order:
  1. `"Deleting test results"` (`TestResults` by `TestRun.AssignmentId` / `Test.AssignmentId`)
  2. `"Deleting solution files"` (`SolutionFiles` by `AssignmentId`)
  3. `"Deleting peer assessments"` (`PeerAssessments` by `ProjectTeamAssessmentId`)
  4. `"Deleting assessment peer links"` (`ProjectTeamAssessmentPeers` by `ProjectTeamAssessmentId`)
  5. `"Deleting gamification events"` (`GamificationEvents` by `StudentCourseProfileId`)
  6. `"Deleting student badges"` (`StudentBadges` by `StudentCourseProfileId`)
  7. `"Deleting test runs"` (`TestRuns` by `AssignmentId`)
  8. `"Deleting tests"` (`Tests` by `AssignmentId`)
  9. `"Deleting test code hashes"` (`TestCodeHashes` by `AssignmentId`)
  10. `"Deleting project team assessments"` (`ProjectTeamAssessments` by `ProjectAssessmentId` / `ProjectTeamId`)
  11. `"Deleting project assessments"` (`ProjectAssessments` by `ProjectId`)
  12. `"Deleting project team memberships"` (`ProjectTeamUsers` by `ProjectTeamId`)
  13. `"Deleting project teams"` (`ProjectTeams` by `ProjectId`)
  14. `"Deleting assignments"` (`Assignments` by `TopicId`)
  15. `"Deleting student course profiles"` (`StudentCourseProfiles` by `CourseId` or `PeriodId`)
  16. `"Deleting course class groups"` (`CourseClassGroups` by `CourseId` or `PeriodId`)
  17. `"Deleting chapters and projects"` (`Topics` by `CourseId` or `PeriodId`)
  18. `"Deleting course gamification config"` (`CourseGamificationConfigs` by `CourseId`, when `TargetType == Course`)
  19. `"Removing course"` / `"Removing period"` (root row in `Courses` or `Periods`)

---

## 5. API Layer (`Guts.Api`)

### Controllers & Models
- **`CourseController.DeleteCourse(int courseId)`**:
  - `[HttpDelete("{courseId}")]`, `[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]`
  - Returns `202 Accepted` (`ProducesResponseType(typeof(DeletionJobStatusModel), 202)`) with mapped `DeletionJobStatusModel`.
- **`PeriodController.DeletePeriod(int id)`**:
  - `[HttpDelete("{id}")]`, `[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]`
  - Returns `202 Accepted` (`ProducesResponseType(typeof(DeletionJobStatusModel), 202)`) with mapped `DeletionJobStatusModel`.
- **`DeletionJobController`** (`Guts.Api/Controllers/DeletionJobController.cs`, `[Route("api/deletion-jobs")]`, `[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]`):
  - `GET /api/deletion-jobs/{jobId}` -> `200 OK` (`DeletionJobStatusModel`) or `404 Not Found`
  - `GET /api/deletion-jobs/active` (optional `[FromQuery] string targetType = null`) -> `200 OK` (`IList<DeletionJobStatusModel>`) or `400 Bad Request` on invalid `targetType`
- **`DeletionJobStatusModel`** & `IMapper.MapToDeletionJobStatusModel(DeletionJobDto dto)`.

### Background Worker & Queue
- **`InMemoryDeletionJobQueue`** (`Guts.Api/BackgroundServices/InMemoryDeletionJobQueue.cs`) registered as singleton `IDeletionJobQueue`.
- **`DeletionBackgroundService`** (`Guts.Api/BackgroundServices/DeletionBackgroundService.cs`):
  - Runs startup pass for any `Pending`/`InProgress` jobs in the DB, then awaits queue notifications via `IDeletionJobQueue.DequeueAllAsync(...)` (calling `IDeletionJobService.ProcessPendingJobsAsync(batchSize, stoppingToken)` inside a fresh DI scope).
  - Batch size configurable via `Deletion:BatchSize` (default `1000`); toggleable via `Deletion:Enabled` (default `true`).

---

## 6. Frontend Layer (`Guts.Web/ClientApp`)

### Services & Models
- **`IDeletionJobStatusModel`** (`src/app/viewmodels/deletion-job.model.ts`)
- **`DeletionJobService`** (`src/app/services/deletion-job.service.ts`):
  - `getJobStatus(jobId: string): Observable<GetResult<IDeletionJobStatusModel>>`
  - `getActiveJobs(targetType: "Course" | "Period"): Observable<GetResult<IDeletionJobStatusModel[]>>`
  - `pollUntilComplete(jobId: string, pollIntervalMs = 500): Observable<GetResult<IDeletionJobStatusModel>>`
- **`CourseService.deleteCourse(courseId)` & `PeriodService.deletePeriod(periodId)`**:
  - Return `Observable<GetResult<IDeletionJobStatusModel>>` (parsing the `202 Accepted` body).

### Shared Modal & Admin Components
- **`DangerDeleteModalComponent`**:
  - Add optional inputs: `deletionInProgress = input<boolean>(false)`, `progressPercentage = input<number>(0)`, `progressLabel = input<string>("")`.
  - When `deletionInProgress()` is `true`: lock modal controls (Escape / Close `×` / Cancel / Confirm disabled), suppress opaque loading overlay so the modal body stays readable, hide confirmation input, and render a Bootstrap `.progress` bar (`role="progressbar"`, `[style.width.%]="progressPercentage()"`, `aria-valuenow`) + `progressLabel()`.
- **`AdminCoursesComponent` & `AdminPeriodsComponent`**:
  - On `ngOnInit`, check `getActiveJobs("Course" | "Period")`: if an active job is found after reload, open the locked progress modal for that job and resume polling until completion.
  - On `confirmDelete()`, keep `coursePendingDelete` / `periodPendingDelete` open in locked progress mode (`deletionInProgress = true`), start polling via `DeletionJobService`, update progress percentage & phase label in real time, then toast + close + reload list on `Completed` (or toast error + close/unlock on `Failed`).

---

## 7. Dependency Injection (`Guts.Bootstrapper` & `Guts.Api`)

- `DeletionJobFactory` in `Guts.Domain` matches `*Factory` -> auto-registered Singleton.
- `DeletionJobService` and `DeletionJobConverter` in `Guts.Application` match `*Service` / `*Converter` -> auto-registered Scoped / Singleton.
- `DeletionJobDbRepository` and `ChunkedDeletionDbRepository` in `Guts.Infrastructure` match `*Repository` -> auto-registered Scoped.
- Explicit registrations in `Guts.Api/Startup.cs`:
  - `services.AddSingleton<IDeletionJobQueue, InMemoryDeletionJobQueue>();`
  - `services.AddHostedService<DeletionBackgroundService>();`
