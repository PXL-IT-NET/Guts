# Specification: Course Statistics Visualization & Breakdown Ordering

- **Feature ID**: `008-course-statistics`
- **Status**: `Implemented`
- **Created**: `2026-11-19`

---

## 1. Context & Problem Statement

Lectors teaching a course in GUTS need a centralized overview of how all students in a given academic period are progressing across the course's chapters and exercises (assignments). The **Course Statistics** view (`/courses/:courseId/statistics`) aggregates the latest test results across all chapter assignments of a course in a period, displays high-level course totals, lists all students who have submitted at least one test result in a sortable table, and lets lectors expand any student row (accordion) to inspect that student's chapter-by-chapter and assignment-by-assignment test pass breakdown.

### Bug to Resolve

Currently, when a lector expands a student's accordion row in the Course Statistics view, the chapters and assignments inside the breakdown are displayed in the wrong order because `CourseProgressCalculator` sorts both chapters and assignments by `Code` (`OrderBy(c => (string)c.Code)` and `OrderBy(a => a.Code)`), and the frontend renders the returned arrays without applying description-based ordering. In GUTS courses, lectors commonly customize chapter and assignment descriptions to express the intended curriculum sequence (matching the Chapter dropdown in `CourseComponent` and the exercise list in `ChapterDetailModel`).

To fix this bug, chapters in the student breakdown must be sorted by **Chapter `Description`** (falling back to `Code` when `Description` is empty or whitespace), and assignments within each chapter must likewise be sorted by **Assignment `Description`** (falling back to `Code` when `Description` is empty or whitespace).

---

## 2. User Stories & Actors

- **Actors**: `Lector (Teacher) | Student`

- **US-1**: As a **Lector**, I want to open a Course Statistics tab on a course so that I can monitor overall student progress across all chapters and assignments in the selected academic period.
- **US-2**: As a **Lector**, I want to sort students by last name, first name, number of assignments worked on, or course completion percentage so that I can quickly identify top performers or students who are falling behind.
- **US-3**: As a **Lector**, I want to expand a student's accordion row to see their per-chapter and per-assignment test results in the logical curriculum order (sorted by description, with code as fallback) so that the breakdown matches the chapter and exercise order used elsewhere in the course.
- **US-4**: As a **Lector**, I want the Course Statistics view to reload automatically when I switch the selected academic period so that I can compare or review progress from previous periods.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Course Statistics Tab Visibility & Navigation
- **Given** an authenticated user viewing a course (`/courses/:courseId`)
- **When** the course contents have loaded
- **Then**:
  - The top-level **"Course statistics"** tab (`routerLink="/courses/:courseId/statistics"`) is rendered **if and only if** the user has the `lector` role (`userProfile.isLector()`) **and** the course has at least one assignment in any chapter or project (`hasAssignments`).
  - Students (and users without the `lector` role) do **not** see the Course statistics tab.
  - Clicking the tab navigates to `/courses/:courseId/statistics` and marks the tab as active.

### AC-2: Course Statistics API Retrieval & Domain Progress Calculation
- **Given** an authenticated `lector` requesting `GET /api/courses/{courseId}/statistics` (with optional query parameter `periodId`)
- **When** the backend processes the request via `CourseController.GetCourseStatistics` ➔ `CourseService.GetCourseStatisticsAsync` ➔ `CourseProgressCalculator.CalculateProgress`
- **Then** the response returns `200 OK` with a `CourseStatisticsModel` containing:
  - Course metadata: `courseId`, `courseCode`, `courseName`.
  - Course totals for the period (scoped to `Chapter` assignments): `totalAssignments` (count of all assignments across the course's chapters in that period) and `totalTests` (count of distinct tests across those assignments).
  - `students`: one `CourseStudentStatisticsModel` per student who has at least one latest `TestResult` for any of those assignments (students with no submitted test results in the period are omitted; if `totalAssignments == 0`, `students` is empty).
  - For each student:
    - `studentId`, `firstName`, `lastName`, `fullName` (`"{firstName} {lastName}".Trim()`).
    - `assignmentsWorkedOn`: count of assignments in the course for which the student has at least one latest test result.
    - `totalPassedTests`: sum of passed latest tests belonging to each assignment's test set.
    - `totalTests`: equal to the course's `totalTests`.
    - `completionPercentage`: `Math.Round((double)totalPassedTests / totalTests * 100.0, 2)` when `totalTests > 0`, otherwise `0.0`.
    - Default student ordering from the API: `LastName` ascending, then `FirstName` ascending.
    - `chapters`: all chapters of the course in the period, each containing all its assignments with `assignmentId`, `code`, `description`, `numberOfPassedTests`, and `numberOfTests`.

### AC-3: Period Selection & Loading State
- **Given** a `lector` is on `/courses/:courseId/statistics`
- **When** the component initializes or the selected period changes in `PeriodProvider.period$`
- **Then**:
  - A loading overlay is shown while `GET /api/courses/{courseId}/statistics?periodId={periodId}` (omitting `periodId` when `<= 0`) is in flight.
  - On success, the view updates with the statistics for the newly selected period.
  - On API error, the loading overlay is hidden and a toast error notification is displayed (`result.message || "Failed to load course statistics."`).

### AC-4: Overview Summary Cards & Empty State
- **Given** course statistics have loaded in `CourseStatisticsComponent`
- **When** the view renders
- **Then**:
  - Three overview summary cards are displayed:
    - **Assignments**: `model.totalAssignments`
    - **Total tests**: `model.totalTests`
    - **Students with results**: `model.students.length`
  - If `model.students.length === 0`, the student table is hidden and an info alert is displayed: `"No student test results have been submitted for this course in this period yet."`
  - If `model.students.length > 0`, the sortable student progress table is displayed.

### AC-5: Sortable Student Progress Table & Completion Progress Bar
- **Given** `model.students.length > 0`
- **When** the lector views and interacts with the student table
- **Then**:
  - The table displays columns: expand chevron, **Last name**, **First name**, **Assignments worked on** (`{assignmentsWorkedOn} / {totalAssignments}` badge), and **Course completion** (progress bar + `{completionPercentage}% ({totalPassedTests}/{totalTests})`).
  - Default sorting is **Last name** ascending (`asc`), with **First name** as tie-breaker.
  - Clicking an inactive column header sorts ascending on that column (`lastName`, `firstName`, `assignmentsWorkedOn`, or `completionPercentage`); clicking the active column header toggles between `asc` and `desc`.
  - Tie-breakers: `lastName` breaks ties by `firstName`; `firstName`, `assignmentsWorkedOn`, and `completionPercentage` break ties by `lastName`. Text comparisons are case-insensitive (`sensitivity: "base"`).
  - Column header icons indicate inactive (`bi-arrow-down-up text-muted`), ascending (`bi-sort-down text-primary`), and descending (`bi-sort-up-alt text-primary`).
  - The completion progress bar uses contextual color classes:
    - `>= 100%`: `bg-success`
    - `>= 50%` and `< 100%`: `bg-primary`
    - `> 0%` and `< 50%`: `bg-warning`
    - `0%`: `bg-secondary`

### AC-6: Expandable Student Accordion & Assignment Breakdown Display
- **Given** the student progress table is displayed
- **When** the lector clicks a student row (or presses `Enter` while focused on the row)
- **Then**:
  - The row toggles between collapsed (`bi-chevron-right`) and expanded (`bi-chevron-down`, `table-active`). Multiple student rows can be expanded independently at the same time.
  - When expanded, a detail card displays `"Assignment breakdown for {lastName} {firstName}"` followed by one section per chapter:
    - Chapter header displays the chapter's `description` (falling back to `code` if `description` is empty/whitespace).
    - If a chapter has no assignments, `"No assignments in this chapter."` is shown.
    - Otherwise, a nested table lists each assignment with:
      - **Assignment**: `code` (bold), followed by ` - {description}` when `description` is non-empty and differs from `code`.
      - **Green tests**: `{numberOfPassedTests} / {numberOfTests}`.
      - **Status** badge:
        - `numberOfTests === 0` ➔ `No tests` (`bg-secondary`)
        - `numberOfPassedTests === numberOfTests` ➔ `All passed` (`bg-success`)
        - `numberOfPassedTests > 0` ➔ `Partially passed` (`bg-warning text-dark`)
        - `numberOfPassedTests === 0` ➔ `0 passed` (`bg-danger`)

### AC-7: Chapter Sorting in Student Breakdown (Bug Fix)
- **Given** a course with multiple chapters whose `Description` order differs from their `Code` order (and/or chapters where `Description` is null, empty, or whitespace)
- **When** course progress is calculated by `CourseProgressCalculator` and rendered in an expanded student accordion in `CourseStatisticsComponent`
- **Then**:
  - Chapters are sorted using standard case-insensitive alphabetical ascending order (`StringComparer.OrdinalIgnoreCase` in .NET / `localeCompare` with `sensitivity: "base"` in Angular) by their effective display label: `Description` when non-empty/non-whitespace, otherwise `Code`.
  - When two chapters have the same effective display label, `Code` (standard case-insensitive alphabetical ascending) acts as the deterministic tie-breaker.

### AC-8: Assignment Sorting within Each Chapter (Bug Fix)
- **Given** a chapter with multiple assignments whose `Description` order differs from their `Code` order (and/or assignments where `Description` is null, empty, or whitespace)
- **When** course progress is calculated by `CourseProgressCalculator` and rendered in an expanded student accordion in `CourseStatisticsComponent`
- **Then**:
  - Assignments within each chapter are sorted using standard case-insensitive alphabetical ascending order (`StringComparer.OrdinalIgnoreCase` in .NET / `localeCompare` with `sensitivity: "base"` in Angular) by their effective display label: `Description` when non-empty/non-whitespace, otherwise `Code`.
  - When two assignments have the same effective display label, `Code` (standard case-insensitive alphabetical ascending) acts as the deterministic tie-breaker.
  - Note: Because `Assignment.Description` automatically formats positive numeric codes (e.g., `"1"` ➔ `"Assignment 1"`) when no explicit description is set, sorting by `Description` with `Code` fallback naturally uses the resolved `Assignment.Description` value (falling back to `Code` if still blank).

---

## 4. Domain Rules & Invariants

- **DR-1**: `CourseProgressCalculator.CalculateProgress(course, chapters, testResults)` enforces `Contracts.Require(course != null, "Course cannot be null.")`. Null `chapters` or `testResults` collections are safely treated as empty.
- **DR-2**: Only `Chapter` assignments (individual exercises) are included in `CourseProgressReport`; `Project` components are not part of per-student course statistics.
- **DR-3**: A student's `TotalPassedTests` only counts latest `TestResult` records where `Passed == true` and `TestId` belongs to the assignment's current `Tests` collection.
- **DR-4**: `CompletionPercentage` is rounded to 2 decimal places (`Math.Round(..., 2)`) and defaults to `0.0` when `TotalTests == 0`.
- **DR-5**: Canonical chapter ordering inside `CourseStudentProgress.Chapters` is standard case-insensitive alphabetical ascending (`StringComparer.OrdinalIgnoreCase`) by effective sort key `string.IsNullOrWhiteSpace(chapter.Description) ? (string)chapter.Code : chapter.Description`, then by `(string)chapter.Code`.
- **DR-6**: Canonical assignment ordering inside `CourseChapterProgress.Assignments` is standard case-insensitive alphabetical ascending (`StringComparer.OrdinalIgnoreCase`) by effective sort key `string.IsNullOrWhiteSpace(assignment.Description) ? assignment.Code : assignment.Description`, then by `assignment.Code`.

---

## 5. Authorization & Security Rules

- **Students**:
  - Do not see the "Course statistics" navigation tab in `CourseComponent`.
  - Direct navigation to `/courses/:courseId/statistics` triggers a client-side guard check in `CourseStatisticsComponent` that shows an error toast (`"Only lectors can view course statistics."`) and redirects to `/courses/:courseId`.
  - Direct API calls to `GET /api/courses/{courseId}/statistics` are rejected by `[Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]` with `403 Forbidden`.
- **Lectors**:
  - Users holding the `lector` role are authorized to access `GET /api/courses/{courseId}/statistics` and view `/courses/:courseId/statistics`.
- **Administrators (without `lector` role)**:
  - `LectorsOnlyPolicy` requires the `lector` role specifically (non-lector admins receive `403 Forbidden`, matching current behavior).
- **Unauthenticated Users**:
  - Frontend route `/courses/:courseId/statistics` is protected by `authGuard` (redirects to `/login`).
  - API endpoint returns `401 Unauthorized`.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| `courseId < 1` on `GET /api/courses/{courseId}/statistics` | `CourseController` rejects invalid ID | `400 Bad Request` |
| `course` or `period` not found | Repository throws `DataNotFoundException`; controller returns NotFound | `404 Not Found` |
| `CalculateProgress` called with `null` `Course` | Throws `ContractException` (`Contracts.Require`) | N/A (Domain) |
| Authenticated non-lector accesses endpoint | Blocked by `LectorsOnlyPolicy` | `403 Forbidden` |
| Unauthenticated caller accesses endpoint | Blocked by JWT auth middleware | `401 Unauthorized` |
| Course has 0 chapters or 0 assignments | Returns `totalAssignments: 0`, `totalTests: 0`, `students: []`; UI shows empty-state alert | `200 OK` |
| Course has assignments with 0 tests (`totalTests == 0`) | `completionPercentage` is `0.0`; assignment status shows `No tests` badge | `200 OK` |
| Chapter has no `description` (`null` / `""` / whitespace) | Sorted by `code`; chapter heading in accordion falls back to `code` | `200 OK` |
| Assignment has no `description` (`null` / `""` / whitespace) | Sorted by `code`; assignment cell renders `code` only without ` - ` suffix | `200 OK` |
| Test results contain entries with `User == null` | Ignored during grouping in `CourseProgressCalculator` | `200 OK` |

---

## 7. Non-Functional & Quality Gate Requirements

- **Test Coverage**: Unit tests must cover all new and modified lines and branches across `Guts.Domain.Tests` (`CourseProgressCalculatorTests`) and `Guts.Web/ClientApp` (`coursestatistics.component.spec.ts`), satisfying the SonarCloud new-code coverage Quality Gate (`>= 80%`, targeting `~100%`).
- **Playwright E2E Coverage**: Add Playwright E2E test scenarios in `Backoffice/Guts.Web/ClientApp/e2e/tests/` covering the Course Statistics page: overview cards, student sorting, expanding a student accordion, and verifying that chapters and assignments appear sorted by description (with code fallback).
- **Static Analysis & Security**: SonarCloud / SonarAnalyzer checks must pass with `0` open issues, `0` unreviewed security hotspots, and `0` ESLint errors.

---

## 8. Out of Scope

- Including Project components / team test results in Course Statistics (remains scoped to individual Chapter assignments).
- Exporting Course Statistics to CSV/Excel or filtering/searching students by name.
- Changing how `ChapterConverter`, `ProjectConverter`, or `TopicConverter` sort chapters/assignments in other endpoints outside Course Statistics.
