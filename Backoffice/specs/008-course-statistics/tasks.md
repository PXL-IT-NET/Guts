# TDD Implementation Plan: Course Statistics Visualization & Breakdown Ordering

- **Feature ID**: `008-course-statistics`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-7, AC-8, DR-5, DR-6]` Write failing unit tests in `Backoffice/Guts.Domain.Tests/CourseAggregate/CourseProgressCalculatorTests.cs` covering:
  - Sorting chapters by `Description` (case-insensitive alphabetical ascending) when `Description` order differs from `Code` order.
  - Falling back to chapter `Code` when `Description` is `null`, empty, or whitespace, and breaking equal-description ties by `Code`.
  - Sorting assignments within each chapter by `Description` (case-insensitive alphabetical ascending) when `Description` order differs from `Code` order.
  - Falling back to assignment `Code` when `Description` is `null`, empty, or whitespace, and breaking equal-description ties by `Code`.
- [x] **[T1.2]** `[AC-7, AC-8, DR-5, DR-6]` Implement `GetSortKey(description, code)` and update chapter and assignment ordering in `Backoffice/Guts.Domain/CourseAggregate/CourseProgressCalculator.cs` using `StringComparer.OrdinalIgnoreCase` with `Code` tie-breaker.
- [x] **[T1.3]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phases 2–4: Application, Infrastructure & API Layers

- _No production changes required — `CourseService`, `ChapterDbRepository`, `TestResultDbRepository`, `CourseController`, and `CourseConverter` already preserve collection ordering end-to-end._

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T5.1]** `[AC-6, AC-7, AC-8]` Write failing Jasmine unit tests in `Backoffice/Guts.Web/ClientApp/src/app/components/coursestatistics/coursestatistics.component.spec.ts` covering:
  - `getSortedChapters(student)`: sorts by `description` (case-insensitive alphabetical), falls back to `code` when `description` is empty/whitespace/undefined, breaks ties by `code`, and handles empty/missing chapters.
  - `getSortedAssignments(chapter)`: sorts by `description` (case-insensitive alphabetical), falls back to `code` when `description` is empty/whitespace/undefined, breaks ties by `code`, and handles empty/missing assignments.
  - `getChapterTitle(chapter)`: returns trimmed `description` or falls back to `code` when `description` is empty/whitespace.
  - Rendered DOM order in an expanded student accordion row verifies chapters, chapter fallback headings, and assignments appear in the expected sorted order.
- [x] **[T5.2]** `[AC-6, AC-7, AC-8]` Implement `getSortedChapters`, `getSortedAssignments`, `getChapterTitle`, and `compareByDescriptionThenCode` in `Backoffice/Guts.Web/ClientApp/src/app/components/coursestatistics/coursestatistics.component.ts` and update `coursestatistics.component.html`.
- [x] **[T5.3]** `[AC-1, AC-4, AC-5, AC-6, AC-7, AC-8]` Add Playwright E2E coverage:
  - Add `mockCourseStatistics` and `**/statistics**` route handling in `Backoffice/Guts.Web/ClientApp/e2e/mocks/course.mocks.ts`.
  - Add Page Object Model `Backoffice/Guts.Web/ClientApp/e2e/pages/course-statistics.po.ts`.
  - Add E2E spec `Backoffice/Guts.Web/ClientApp/e2e/tests/course-statistics.spec.ts` covering overview cards, student sorting, expanded accordion chapter & assignment order (including empty description code fallback), empty state, and non-lector redirection.
- [x] **[T5.4]** Run `npm run lint`, `npm test -- --watch=false`, `npm run build`, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp` and verify all pass.

---

## Phase 6: Final Verification & Quality Gate

- [x] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [x] **[T6.2]** Run frontend production build, lint, unit tests, and Playwright E2E tests (`e2e/tests/course-statistics.spec.ts` and `e2e/tests/course-progress.spec.ts`).
- [x] **[T6.3]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and verify Quality Gate passes (`>= 80%` new-code coverage aiming near `100%`, `0` open issues, `0` unreviewed hotspots).
- [x] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
