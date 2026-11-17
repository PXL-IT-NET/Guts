# TDD Implementation Plan: Secure In-Browser Monaco Coding Exams

- **Feature ID**: `007-monaco-coding-exams`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-1, AC-2, DR-1, DR-2, DR-3]` Write failing unit tests in `Guts.Domain.Tests/ExamAggregate/` for value objects and catalog:
  - `ExamAccessCodeTests.cs` (trimming, length bounds `4..32`, constant-time `Matches`, immutability)
  - `ExamFilePathTests.cs` (backslash-to-slash normalization, `./` trimming, `..` traversal rejection, leading `/` rejection, `bin/`/`obj/`/`.git/` rejection, immutability)
  - `ExamTemplateCatalogTests.cs` (`IsSupported`, `GetByKey`, `SupportsWasmExecution` flags)
- [x] **[T1.2]** `[AC-1, AC-2, DR-1, DR-2, DR-3]` Implement `ExamStatus`, `ExamFileRole`, `ExamTemplateProfile`, `ExamTemplateCatalog`, `ExamAccessCode`, and `ExamFilePath` in `Guts.Domain/ExamAggregate/`.
- [x] **[T1.3]** `[AC-1, AC-2, AC-3, DR-1, DR-2, DR-3, DR-4]` Create `ExamBuilder` in `Guts.Domain.Tests/Builders/` and write failing unit tests in `Guts.Domain.Tests/ExamAggregate/ExamTests.cs`, `ExamQuestionTests.cs`, and `ExamFactoryTests.cs` covering:
  - Factory creation invariants (`ContractException` on invalid `CourseId`, `PeriodId`, `CreatedByUserId`, empty `Title`, invalid `TemplateKey`, invalid `AccessCode`, non-UTC dates)
  - Question management (`AddQuestion`, `UpdateDetails`, `UpsertFile`, `SetFileRole`, `ReplaceFiles`, `RemoveFile`, duplicate path prevention)
  - Status transitions (`Draft -> Open`, `Open -> Closed`, `Closed -> Open`), rejecting `Open` when no questions or missing `Editable` file
  - `EnsureCanEnter(suppliedAccessCode)` rejecting `Draft` / `Closed` exams or mismatched access codes
- [x] **[T1.4]** `[AC-1, AC-2, AC-3, DR-1, DR-2, DR-3, DR-4]` Implement `ExamQuestionFile`, `ExamQuestion`, `Exam`, `IExamFactory`, and stateless `ExamFactory` in `Guts.Domain/ExamAggregate/`.
- [x] **[T1.5]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Business Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-1, AC-2, AC-3]` Define `GutsClaimTypes.ExamGuest` / `GutsClaimTypes.ExamId`, `CreateExamGuestToken(int examId, int expirationMinutes)` on `ITokenAccessPassFactory`, `IExamRepository`, `IExamProjectImporter`, `Exam` DTOs (`ExamSummaryDto`, `ExamDetailDto`, `ExamQuestionDto`, `ExamQuestionFileDto`, `ExamWorkspaceDto`, `ImportedProjectFileDto`, `ExamEntryResultDto`, `FileRoleAssignmentDto`), `IExamConverter`, and `IExamService`.
- [x] **[T2.2]** `[AC-3]` Write failing unit tests in `Guts.Application.Tests/Security/JwtSecurityTokenAccessPassFactoryTests.cs` verifying exam guest tokens contain `exam_guest = "true"`, `exam_id`, `jti`, and **no** user `NameId`, `Sub`, `Email`, or `Role` claims.
- [x] **[T2.3]** `[AC-1, AC-2, AC-3]` Write failing unit tests in `Guts.Application.Tests/Converters/ExamConverterTests.cs` and `Guts.Application.Tests/Services/Exam/ExamServiceTests.cs` covering:
  - CRUD, Open/Close orchestration, Question CRUD, Zip/Git import orchestration, File role updates
  - `EnterExamAsync` (delegating to `exam.EnsureCanEnter(...)` and issuing exam guest token without exposing `AccessCode`)
  - `GetExamWorkspaceAsync` (verifying `Open` check for guest vs preview for Lector, excluding `AccessCode`)
- [x] **[T2.4]** `[AC-1, AC-2, AC-3]` Implement `JwtSecurityTokenAccessPassFactory.CreateExamGuestToken`, `ExamConverter`, and `ExamService` in `Guts.Application/`.
- [x] **[T2.5]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-1, AC-2]` Add `internal` `ExamConfiguration`, `ExamQuestionConfiguration`, and `ExamQuestionFileConfiguration` in `Guts.Infrastructure/EntityConfigurations/`, add `DbSet<Exam> Exams` to `GutsContext`, and scaffold `AddCodingExams` EF Core migration.
- [x] **[T3.2]** `[AC-1, AC-2]` Write failing integration/unit tests in `Guts.Infrastructure.Tests/Repositories/ExamDbRepositoryTests.cs` and `Guts.Infrastructure.Tests/Exam/ExamProjectImporterTests.cs` covering:
  - `ExamDbRepository` CRUD, course/period filtering, eager loading of ordered questions & files
  - `ExamProjectImporter.ExtractFromZipAsync` (normalizing paths, filtering `bin/`/`obj/`/`.git/` and binary files, auto-classifying `*Tests*`, `.csproj`/`.sln`, and `.cs` files, rejecting zip-slip `../`, oversized archives, and excess file counts)
  - `ExamProjectImporter.CloneFromGitAsync` (rejecting non-HTTPS schemes, `file://`, and private/loopback URLs)
- [x] **[T3.3]** `[AC-1, AC-2]` Implement `ExamDbRepository`, `ExamProjectImporter`, and explicit DI registration for `IExamProjectImporter` in `Guts.Bootstrapper/StartUpExtensions.cs`.
- [x] **[T3.4]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`) & WASM Compiler (`Guts.ExamCompiler.Wasm`)

- [x] **[T4.1]** `[AC-3]` Write failing unit tests in `Guts.Api.Tests/Filters/ExamGuestLockdownFilterTests.cs` verifying:
  - Regular user JWTs pass untouched
  - Exam guest JWTs (`exam_guest = "true"`) are blocked (`403 Forbidden`) on endpoints without `[AllowExamGuest]`
  - Exam guest JWTs are blocked (`403 Forbidden`) when route `examId` does not match `exam_id` claim
  - Exam guest JWTs succeed on `[AllowExamGuest]` endpoints with matching `examId`
- [x] **[T4.2]** `[AC-3]` Implement `AllowExamGuestAttribute` and `ExamGuestLockdownFilter` in `Guts.Api/Filters/` and register globally in `Guts.Api/Startup.cs`.
- [x] **[T4.3]** `[AC-1, AC-2, AC-3]` Write failing unit tests in `Guts.Api.Tests/Controllers/ExamControllerTests.cs` and `Guts.Api.Tests/Controllers/ExamSessionControllerTests.cs` covering authorization policies (`LectorOrAdminPolicy`, course ownership checks, `[AllowAnonymous]` enter, `[AllowExamGuest]` workspace), validation errors, and `IMapper` model conversions.
- [x] **[T4.4]** `[AC-1, AC-2, AC-3]` Implement request/response models, `IMapper` mappings, `ExamController`, and `ExamSessionController` in `Guts.Api/`.
- [ ] **[T4.5]** `[AC-4, AC-5]` Write failing unit tests in `Guts.ExamCompiler.Wasm.Tests/` for Roslyn completion, signature help, hover, diagnostics, C# compilation, and pure `.NET` test/Main execution; implement `Guts.ExamCompiler.Wasm` (`[JSExport]` bridge + curated `.NET 10` reference assembly bundle) and configure build output into `Guts.Web/ClientApp/src/assets/wasm-compiler/` and `wasm-refs/`.
- [x] **[T4.6]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [ ] **[T5.1]** `[AC-4, AC-7]` Install `monaco-editor` and `jszip` in `Backoffice/Guts.Web/ClientApp/package.json` and configure `angular.json` assets for self-hosted Monaco (`assets/monaco/vs`) and WASM compiler/reference bundles (zero external CDNs).
- [ ] **[T5.2]** `[AC-1, AC-2, AC-3, AC-4, AC-5, AC-6, AC-7]` Write failing Jasmine unit tests (`*.spec.ts`) covering all lines and branches for:
  - `src/app/services/exam-management.service.spec.ts`
  - `src/app/services/exam-session.service.spec.ts` & `src/app/interceptors/tokeninterceptor.spec.ts` (exam guest token routing vs regular auth token isolation)
  - `src/app/services/exam-draft-storage.service.spec.ts` (`IndexedDB` persistence, `localStorage` fallback, debounced saves, reset to starter code, clear local draft, 7-day TTL purge)
  - `src/app/services/exam-export.service.spec.ts` (`JSZip` archive generation excluding `bin/`/`obj/`, `// === File: <path> ===` clipboard formatter)
  - `src/app/services/roslyn-wasm.service.spec.ts` (Monaco language providers, diagnostics, build/run, worker `terminateAndRestart()`)
  - `src/app/components/app.component.spec.ts` (hiding `<app-nav-menu>` & `<app-sidebar-menu>` on `examShell: true` routes)
  - Teacher components: `CourseExamsComponent`, `ExamEditorComponent` (`*.spec.ts`)
  - Student Exam Shell components: `ExamShellComponent`, `ExamEntryCardComponent`, `ExamWorkspaceComponent` (`*.spec.ts`)
- [ ] **[T5.3]** `[AC-1, AC-2, AC-3, AC-4, AC-5, AC-6, AC-7]` Implement `exam.model.ts`, services, `roslyn-worker.ts`, `TokenInterceptor` update, `AppComponent` examShell support, routes in `app-routing.module.ts`, and all Teacher & Student Exam Shell components.
- [ ] **[T5.4]** `[AC-1..AC-7]` Add Playwright E2E tests in `Backoffice/Guts.Web/ClientApp/e2e/tests/` (and Page Objects/mocks in `e2e/pages/`, `e2e/mocks/`) covering:
  - Lector creating an exam, importing `.zip`, tagging file roles, opening exam, and previewing Exam Shell
  - Student entering `/exam/{examId}` with Access Code (invalid vs valid), distraction-free Exam Shell without nav/sidebar, and API lockdown (`403` on regular endpoints)
  - Monaco editing, deterministic build & public test execution, refresh recovery from `IndexedDB`/`localStorage`, Reset to Starter Code, `.zip` download, and Copy All Editable Code fallback
- [ ] **[T5.5]** Run `npm run lint`, `npm test -- --watch=false`, `npm run build`, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp` and verify all pass.

---

## Phase 6: Final Verification & Quality Gate

- [ ] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [ ] **[T6.2]** Run frontend production build, lint, unit tests, and Playwright E2E tests (`npm run lint`, `npm test -- --watch=false`, `npm run build`, `npm run test:e2e`).
- [ ] **[T6.3]** Verify zero external CDN requests in Network tab / Playwright assertions when running `/exam/:examId` (Schoolyear compatibility).
- [ ] **[T6.4]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and inspect per-file new-code coverage; add missing tests for uncovered lines/branches across backend and frontend until Quality Gate is `OK` (maximum achievable new-code coverage, `0` open issues, `0` unreviewed hotspots).
- [ ] **[T6.5]** Update `spec.md` and `design.md` status to `Implemented`.
