# Technical Design: Course Statistics Visualization & Breakdown Ordering

- **Feature ID**: `008-course-statistics`
- **Specification**: [`spec.md`](./spec.md)
- **Status**: `Implemented`

---

## 1. Architectural Overview

The Course Statistics feature spans the full GUTS Backoffice stack:

```text
Angular SPA (CourseComponent ➔ CourseStatisticsComponent ➔ CourseService)
        │  GET /api/courses/{courseId}/statistics?periodId={periodId}
        ▼
Guts.Api (CourseController.GetCourseStatistics ➔ CourseConverter.ToCourseStatisticsModel)
        │
        ▼
Guts.Application (CourseService.GetCourseStatisticsAsync)
        ├──► Guts.Infrastructure (CourseDbRepository, PeriodDbRepository, ChapterDbRepository, TestResultDbRepository)
        └──► Guts.Domain (CourseProgressCalculator.CalculateProgress ➔ CourseProgressReport)
```

- **Canonical ordering (`DR-5`, `DR-6`)** belongs in `Guts.Domain` (`CourseProgressCalculator`), ensuring that every consumer of `CourseProgressReport` (REST API, future exports, or MCP tools) receives chapters and assignments sorted by description (with code fallback).
- **Defensive UI sorting & header fallback (`AC-6`, `AC-7`, `AC-8`)** are implemented in `CourseStatisticsComponent` so the accordion always renders chapters and assignments in standard case-insensitive alphabetical order by description (falling back to code) and never displays a blank chapter header when `description` is empty or whitespace.
- **`Guts.Application`, `Guts.Infrastructure`, `Guts.Api`, and `Guts.Bootstrapper`** require no structural changes; their existing contracts already preserve collection order end-to-end.

---

## 2. Domain Layer (`Guts.Domain`)

### Domain Service (`Guts.Domain/CourseAggregate/`)

- **Modified Type**: `CourseProgressCalculator` (`Guts.Domain/CourseAggregate/CourseProgressCalculator.cs`) implementing `ICourseProgressCalculator`
  - **Existing invariant (`DR-1`)**: `Contracts.Require(course != null, "Course cannot be null.");`
  - **Change (`DR-5`, `DR-6`)**:
    - Extract a private static helper to resolve the effective sort key (DRY):
      ```csharp
      private static string GetSortKey(string description, string code)
      {
          return string.IsNullOrWhiteSpace(description)
              ? code ?? string.Empty
              : description;
      }
      ```
    - Pre-sort chapters once before iterating over `userGroups` using `StringComparer.OrdinalIgnoreCase`:
      ```csharp
      var orderedChapters = chapters
          .OrderBy(c => GetSortKey(c.Description, c.Code), StringComparer.OrdinalIgnoreCase)
          .ThenBy(c => (string)c.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
          .ToList();
      ```
    - Sort assignments within each chapter using the same helper and `StringComparer.OrdinalIgnoreCase`:
      ```csharp
      var orderedAssignments = (chapter.Assignments ?? Enumerable.Empty<Assignment>())
          .OrderBy(a => GetSortKey(a.Description, a.Code), StringComparer.OrdinalIgnoreCase)
          .ThenBy(a => a.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase);
      ```
  - **Unchanged Types**: `CourseProgressReport`, `CourseStudentProgress`, `CourseChapterProgress`, `CourseAssignmentProgress` remain immutable read-only models.

---

## 3. Application Layer (`Guts.Application`) — Use-Case Orchestration Only

> [!IMPORTANT]
> Zero domain logic allowed here. No code changes are required in `Guts.Application`.

- **Existing Orchestration**: `CourseService.GetCourseStatisticsAsync(int courseId, int? periodId = null)` (`Guts.Application/Services/CourseService.cs`):
  1. Loads `Course` via `ICourseRepository.GetByIdAsync(courseId)`.
  2. Resolves `Period` via `IPeriodRepository.GetPeriodAsync(periodId)`.
  3. Loads chapters with assignments and tests via `IChapterRepository.GetByCourseIdWithAssignmentsAndTestsAsync(courseId, period.Id)`.
  4. Loads latest test results across chapter assignment IDs via `ITestResultRepository.GetLastTestResultsOfAssignmentsAsync(assignmentIds, null)`.
  5. Delegates calculation to `ICourseProgressCalculator.CalculateProgress(course, [.. chapters], [.. lastTestResults])`.
  6. Maps `CourseProgressReport` to `CourseStatisticsDto`, preserving the order of `Students`, `Chapters`, and `Assignments`.

---

## 4. Infrastructure Layer (`Guts.Infrastructure`)

- **No changes required.**
- **Migration Required?**: `No`
- **Existing Repositories**:
  - `ChapterDbRepository.GetByCourseIdWithAssignmentsAndTestsAsync` eagerly loads `Assignments` and `Tests`.
  - `TestResultDbRepository.GetLastTestResultsOfAssignmentsAsync` queries the latest `TestResult` per `(TestId, UserId)` with `Test` and `User` included (`AsNoTracking`).

---

## 5. API Layer (`Guts.Api`)

- **No changes required.**
- **Controller**: `CourseController` (`Guts.Api/Controllers/CourseController.cs`)
- **Endpoint**: `GET /api/courses/{courseId}/statistics?periodId={periodId}`
- **Authorization**: `[Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]`
- **Response Model**: `CourseStatisticsModel` (`Guts.Api/Models/CourseStatisticsModel.cs`)
- **Model Converter**: `CourseConverter.ToCourseStatisticsModel(CourseStatisticsDto)` (`Guts.Api/Models/Converters/CourseConverter.cs`) preserves the chapter and assignment ordering produced by `CourseProgressCalculator`.

---

## 6. Frontend Layer (`Guts.Web/ClientApp`)

### Services & ViewModels (Unchanged)
- `CourseService.getCourseStatistics(courseId, periodId)` (`src/app/services/course.service.ts`) calls `GET api/courses/${courseId}/statistics` (appending `?periodId=${periodId}` when `periodId > 0`).
- Interfaces in `src/app/viewmodels/course.model.ts`: `ICourseStatisticsModel`, `ICourseStudentStatisticsModel`, `ICourseStudentChapterStatisticsModel`, `ICourseStudentAssignmentStatisticsModel`.

### Components & Routing
- **Routing (`src/app/app-routing.module.ts`)**: Child route `statistics` under `courses/:courseId`, guarded by `authGuard`.
- **Parent Navigation (`CourseComponent`)**: Displays the "Course statistics" tab when `canShowCourseStatistics` (`userProfile.isLector() && hasAssignments`) is `true`.
- **Modified Component**: `CourseStatisticsComponent` (`src/app/components/coursestatistics/coursestatistics.component.ts` & `.html`)
  - Add reusable sorting & display helpers:
    - `public getSortedChapters(student: ICourseStudentStatisticsModel): ICourseStudentChapterStatisticsModel[]`: returns a shallow sorted copy of `student.chapters` using `compareByDescriptionThenCode`.
    - `public getSortedAssignments(chapter: ICourseStudentChapterStatisticsModel): ICourseStudentAssignmentStatisticsModel[]`: returns a shallow sorted copy of `chapter.assignments` using `compareByDescriptionThenCode`.
    - `public getChapterTitle(chapter: ICourseStudentChapterStatisticsModel): string`: returns `chapter.description?.trim() || chapter.code` so chapters without a description display their `code` instead of a blank heading (`AC-6`).
    - `private compareByDescriptionThenCode(a: { description?: string; code?: string }, b: { description?: string; code?: string }): number`:
      - Resolves effective key `item.description?.trim() || item.code || ""`.
      - Compares effective keys with `localeCompare(..., undefined, { sensitivity: "base" })` (standard case-insensitive alphabetical sorting, `AC-7`, `AC-8`).
      - Breaks ties using `(a.code || "").localeCompare(b.code || "", undefined, { sensitivity: "base" })`.
  - Update `coursestatistics.component.html`:
    - Line 143: `@for (chapter of getSortedChapters(student); track chapter.chapterId)`
    - Line 146: `<span>{{ getChapterTitle(chapter) }}</span>`
    - Line 162: `@for (assignment of getSortedAssignments(chapter); track assignment.assignmentId)`

### Testing Strategy (Frontend)
- **Unit Tests (`coursestatistics.component.spec.ts`)**:
  - Test `getSortedChapters`, `getSortedAssignments`, and `getChapterTitle` across normal descriptions, empty/whitespace/missing descriptions (code fallback), case-insensitive ordering, and equal-description tie-breaking by code.
  - Test rendered DOM order inside an expanded student accordion row to verify template integration.
- **Playwright E2E Tests**:
  - Add `mockCourseStatistics` (with intentionally unsorted chapters/assignments including a chapter and assignment with empty description) and route handler in `e2e/mocks/course.mocks.ts`.
  - Add Page Object Model `e2e/pages/course-statistics.po.ts`.
  - Add `e2e/tests/course-statistics.spec.ts` covering:
    - Lector viewing overview cards, sorting the student table, expanding a student accordion, and verifying chapters and assignments appear in description order with code fallback.
    - Empty state when no students have submitted results.
    - Student redirection when attempting to access `/courses/1/statistics` directly.

---

## 7. Dependency Injection (`Guts.Bootstrapper`)

- No new classes require DI registration. `ICourseProgressCalculator` ➔ `CourseProgressCalculator` is already registered in `Guts.Bootstrapper/StartUpExtensions.cs`.
