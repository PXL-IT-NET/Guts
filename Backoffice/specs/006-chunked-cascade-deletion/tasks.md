# TDD Implementation Plan: Chunked Leaf-to-Root Course & Period Deletion with UI Progress Bar

- **Feature ID**: `006-chunked-cascade-deletion`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-1, DR-1, DR-2, DR-4]` Write failing unit tests in `Guts.Domain.Tests/CourseAggregate/CourseTests.cs` and `Guts.Domain.Tests/PeriodAggregate/PeriodTests.cs` for `IsDeleting` default `false`, `MarkAsDeleting()` transitions, duplicate delete guard (`ContractException`), active-period delete rejection, and update-while-deleting rejection; implement in `Course.cs` and `Period.cs` (`IPeriod`).
- [x] **[T1.2]** `[AC-1, DR-3]` Create `DeletionJobBuilder` in `Guts.Domain.Tests/Builders/` and write failing unit tests in `Guts.Domain.Tests/DeletionJobAggregate/DeletionJobTests.cs` and `DeletionJobFactoryTests.cs` covering creation invariants, `Start`, `RecordBatchProgress`, `ResetForResume`, `Complete`, `Fail`, `CalculatePercentage`, and all `ContractException` guards.
- [x] **[T1.3]** `[AC-1, DR-3]` Implement `DeletionTargetType`, `DeletionJobStatus`, `DeletionJob`, `IDeletionJobFactory`, and `DeletionJobFactory` in `Guts.Domain/DeletionJobAggregate/`.
- [x] **[T1.4]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-2, AC-3, AC-4]` Define `IDeletionJobRepository`, `IChunkedDeletionRepository`, `IDeletionJobQueue`, `DeletionJobDto`, `IDeletionJobConverter`, and `IDeletionJobService` in `Guts.Application/`, and add `QueueDeletionAsync` to `ICourseRepository` & `IPeriodRepository`.
- [x] **[T2.2]** `[AC-2, AC-3, AC-4]` Write failing unit tests in `Guts.Application.Tests/Converters/DeletionJobConverterTests.cs`, `Guts.Application.Tests/Services/Deletion/DeletionJobServiceTests.cs`, `Guts.Application.Tests/Services/CourseServiceTests.cs`, and `Guts.Application.Tests/Services/Period/PeriodServiceTests.cs`.
- [x] **[T2.3]** `[AC-2, AC-3, AC-4]` Implement `DeletionJobConverter`, `DeletionJobService` (including batch progress updates, completion, failure capture, and startup resume handling), and update `CourseService.DeleteCourseAsync` & `PeriodService.DeletePeriodAsync`.
- [x] **[T2.4]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-1]` Add `DeletionJobConfiguration`, configure `Courses.IsDeleting` and `Periods.IsDeleting` (standard table/column DDL only — no `READ_COMMITTED_SNAPSHOT` / `ALTER DATABASE`), add `DbSet<DeletionJob>` to `GutsContext`, and scaffold `AddChunkedDeletionJobs` migration.
- [x] **[T3.2]** `[AC-2, AC-3]` Write failing integration/repository tests in `Guts.Infrastructure.Tests/Repositories/DeletionJobDbRepositoryTests.cs`, `ChunkedDeletionDbRepositoryTests.cs`, `CourseDbRepositoryTests.cs`, and `PeriodDbRepositoryTests.cs` covering:
  - Filtering `IsDeleting == true` out of normal course/period reads (`GetAllAsync`, `GetByIdAsync`, `GetSingleAsync`, `GetPeriodAsync`)
  - Atomic `QueueDeletionAsync`
  - Chunked leaf-to-root deletion across **all 18 child tables + root** for both `Course` and `Period` with small batch sizes (e.g. `batchSize = 2`), verifying non-target course/period data remains intact and incremental progress callbacks fire per chunk
  - Empty course/period deletion (`TotalRows = 1`)
  - `DeletionJobDbRepository` CRUD & active-job queries
- [x] **[T3.3]** `[AC-2, AC-3]` Implement `DeletionJobDbRepository`, `ChunkedDeletionDbRepository`, and update `CourseDbRepository` & `PeriodDbRepository`.
- [x] **[T3.4]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-2, AC-3, AC-4]` Write failing unit tests in:
  - `Guts.Api.Tests/Controllers/DeletionJobControllerTests.cs` (`AdminOnlyPolicy`, `GetById`, `GetActive` valid/invalid target types, `404 Not Found`)
  - `Guts.Api.Tests/Controllers/CourseControllerTests.cs` & `PeriodControllerTests.cs` (`202 Accepted` with `DeletionJobStatusModel`)
  - `Guts.Api.Tests/BackgroundServices/DeletionBackgroundServiceTests.cs` & `InMemoryDeletionJobQueueTests.cs`
- [x] **[T4.2]** `[AC-2, AC-3, AC-4]` Implement `DeletionJobStatusModel`, `IMapper` mapping, `DeletionJobController`, `InMemoryDeletionJobQueue`, `DeletionBackgroundService`, updated `CourseController.DeleteCourse` / `PeriodController.DeletePeriod`, and DI registration in `Startup.cs`.
- [x] **[T4.3]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T5.1]** `[AC-5]` Write failing Jasmine unit tests (`*.spec.ts`) covering all new/modified branches in:
  - `src/app/services/deletion-job.service.spec.ts`
  - `src/app/services/course.service.spec.ts` & `period.service.spec.ts`
  - `src/app/components/danger-delete-modal/danger-delete-modal.component.spec.ts` (locked progress bar rendering, inputs, disabled controls during progress)
  - `src/app/components/admin-courses/admin-courses.component.spec.ts` & `admin-periods/admin-periods.component.spec.ts` (202 initiation, polling updates, completion reload, failure handling, reload active-job recovery, cleanup on destroy)
- [x] **[T5.2]** `[AC-5]` Implement `deletion-job.model.ts`, `DeletionJobService`, updated `CourseService.deleteCourse` & `PeriodService.deletePeriod`, progress-bar mode in `DangerDeleteModalComponent`, and polling/reload recovery in `AdminCoursesComponent` & `AdminPeriodsComponent`.
- [x] **[T5.3]** `[AC-5]` Add Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/` covering Course and Period deletion with live progress bar updates, completion refresh, error handling, and reload active-job recovery.
- [x] **[T5.4]** Run `npm run lint`, `npm test -- --watch=false`, `npm run build`, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp` and verify all pass.

---

## Phase 6: Final Verification & Quality Gate

- [x] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [x] **[T6.2]** Run frontend production build, lint, unit tests, and Playwright E2E tests.
- [x] **[T6.3]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and inspect per-file new-code coverage; add missing tests for uncovered lines/branches across backend and frontend until Quality Gate is `OK` (maximum achievable new-code coverage, `0` open issues, `0` unreviewed hotspots).
- [x] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
