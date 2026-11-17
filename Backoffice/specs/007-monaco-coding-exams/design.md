# Technical Design: Secure In-Browser Monaco Coding Exams

- **Feature ID**: `007-monaco-coding-exams`
- **Specification**: [`spec.md`](./spec.md)
- **Status**: `Approved`

---

## 1. Architectural Overview

This feature introduces a cleanly isolated `ExamAggregate` bounded context inside the `Backoffice` monolith (`Guts.slnx` + `Guts.Web/ClientApp`) with **zero server-side student code storage** and **zero server-side code execution**:

1. **Teacher Authoring Flow**:
   - Authenticated `Lector` / `Administrator` users manage exams via `ExamController` ➔ `IExamService` ➔ `Exam` aggregate & `IExamRepository`.
   - Uploading a `.zip` or importing a Git snapshot uses `IExamProjectImporter` (`Guts.Infrastructure`) to safely extract normalized files (skipping `bin/`, `obj/`, `.git/`) and attach them to `ExamQuestion`s via domain methods on `Exam`.
2. **Student Access-Code Entry & API Lockdown**:
   - Unauthenticated students open `/exam/:examId` in Angular and submit the `AccessCode` to `POST /api/exams/{examId}/enter`.
   - `ExamSessionController` delegates to `IExamService` + `Exam.EnsureCanEnter(...)` and issues a restricted **exam-scoped guest JWT** (`GutsClaimTypes.ExamGuest = "true"`, `GutsClaimTypes.ExamId = "{examId}"`) via `ITokenAccessPassFactory`.
   - A global `ExamGuestLockdownFilter` (`IAsyncAuthorizationFilter`) rejects any request carrying an `ExamGuest` claim to any endpoint not explicitly decorated with `[AllowExamGuest]` (or for a mismatched `examId`) with `403 Forbidden`.
3. **Browser-Only Editing, Roslyn WASM Execution, Local Drafts & Export**:
   - The Angular Exam Shell (`/exam/:examId`) hides `<app-nav-menu>` and `<app-sidebar-menu>`, loads self-hosted Monaco Editor + a single-threaded `.NET 10` Roslyn WASM Web Worker (`Guts.ExamCompiler.Wasm` static assets served from `Guts.Web`), and executes builds/pure `.NET` tests locally in the browser.
   - Student edits to `Editable` files auto-save locally to `IndexedDB` (with `localStorage` fallback) via `ExamDraftStorageService`, and export via `JSZip` (`Download Code (.zip)`) or formatted clipboard text (`Copy Code`).

---

## 2. Domain Layer (`Guts.Domain`)

### Aggregates, Entities & Value Objects
All new domain types live under `Guts.Domain/ExamAggregate/` (pure C#, zero EF Core dependencies, `ValueObject<T>` subclasses immutable with no public setters per `ArchitectureTests`):

- **`ExamStatus`** (`enum`): `Draft = 1`, `Open = 2`, `Closed = 3`.
- **`ExamFileRole`** (`enum`): `Editable = 1`, `ReadOnly = 2`, `PublicTest = 3`.
- **`ExamTemplateProfile`** (`ValueObject<ExamTemplateProfile>` + **`ExamTemplateCatalog`** static registry):
  - Curated `.NET 10` template keys (`dotnet10-console`, `dotnet10-classlib-nunit`, `dotnet10-classlib-xunit`, `dotnet10-aspnetcore-webapi`), display names, and whether runtime execution is supported in WASM (`SupportsWasmExecution: bool`).
  - `ExamTemplateCatalog.IsSupported(string templateKey)` and `GetByKey(string templateKey)`.
- **`ExamAccessCode`** (`ValueObject<ExamAccessCode>`):
  - Private-setter `Value` (`string`).
  - Constructor enforces `Contracts.Require(!string.IsNullOrWhiteSpace(value), ...)` and length bounds (`4..32` chars after trim).
  - `bool Matches(string candidate)` using `CryptographicOperations.FixedTimeEquals` on UTF-8 normalized bytes.
- **`ExamFilePath`** (`ValueObject<ExamFilePath>`):
  - Private-setter `Value` (`string`).
  - Normalizes `\` ➔ `/`, trims leading `./`, enforces non-empty, no `..` path segments, no leading `/`, max length `260`, and rejects `bin/` / `obj/` / `.git/` segments.
- **`ExamQuestionFile`** (`Entity` with `int Id`):
  - Properties (private setters): `Id`, `ExamQuestionId` (FK), `RelativePath` (`ExamFilePath`), `Content` (`string`), `Role` (`ExamFileRole`).
  - Domain methods: `ChangeRole(ExamFileRole role)`, `UpdateContent(string content)` (`Contracts.Require(content != null, ...)`).
- **`ExamQuestion`** (`Entity` with `int Id`):
  - Properties (private setters): `Id`, `ExamId` (FK), `Title` (`string`), `InstructionsMarkdown` (`string`), `OrderIndex` (`int`), `TemplateKey` (`string`), `IReadOnlyCollection<ExamQuestionFile> Files` (private backing `List<ExamQuestionFile> _files = []`).
  - Domain methods:
    - `UpdateDetails(string title, string instructionsMarkdown, int orderIndex, string templateKey)`
    - `UpsertFile(ExamFilePath path, string content, ExamFileRole role)` (prevents duplicate case-insensitive paths)
    - `SetFileRole(int fileId, ExamFileRole role)`
    - `ReplaceFiles(IEnumerable<(ExamFilePath Path, string Content, ExamFileRole Role)> files)`
    - `RemoveFile(int fileId)`
    - `EnsureValidForOpen()` — enforces `!string.IsNullOrWhiteSpace(Title)`, `ExamTemplateCatalog.IsSupported(TemplateKey)`, and `_files.Any(f => f.Role == ExamFileRole.Editable)`.
- **`Exam`** (`AggregateRoot` with `int Id`):
  - Properties (private setters): `Id`, `Title` (`string`), `Description` (`string`), `CourseId` (`int`), `PeriodId` (`int`), `AccessCode` (`ExamAccessCode`), `TemplateKey` (`string`), `Status` (`ExamStatus`), `CreatedByUserId` (`int`), `CreatedAtUtc` (`DateTime`), `UpdatedAtUtc` (`DateTime`), `IReadOnlyCollection<ExamQuestion> Questions` (private backing `List<ExamQuestion> _questions = []`).
  - Domain methods & invariants (`Contracts.Require`):
    - `UpdateMetadata(string title, string description, string accessCode, string templateKey, DateTime utcNow)` (enforces non-empty title, valid `ExamAccessCode`, valid `TemplateKey`, UTC `utcNow`).
    - `AddQuestion(string title, string instructionsMarkdown, int orderIndex, string templateKey)` (enforces unique `OrderIndex` / non-empty title).
    - `GetQuestion(int questionId)` / `RemoveQuestion(int questionId)`.
    - `Open(DateTime utcNow)` (`[DR-1]`): requires `Status is ExamStatus.Draft or ExamStatus.Closed`, `_questions.Count > 0`, calls `q.EnsureValidForOpen()` for every question, sets `Status = ExamStatus.Open`, updates `UpdatedAtUtc`.
    - `Close(DateTime utcNow)` (`[DR-1]`): requires `Status == ExamStatus.Open`, sets `Status = ExamStatus.Closed`, updates `UpdatedAtUtc`.
    - `EnsureCanEnter(string suppliedAccessCode)` (`[DR-3]`): enforces `Status == ExamStatus.Open` and `AccessCode.Matches(suppliedAccessCode)`.

### Domain Factories
- **Interface**: `IExamFactory` (`Guts.Domain/ExamAggregate/IExamFactory.cs`)
- **Implementation**: `ExamFactory` (`Guts.Domain/ExamAggregate/ExamFactory.cs`) — stateless class (no mutable fields, passing `ArchitectureTests.DomainFactories_ShouldBeStateless`), auto-registered as `Singleton` by `StartUpExtensions.AddDomainLayerServices()`.
  - `Exam CreateNew(int courseId, int periodId, int createdByUserId, string title, string description, string accessCode, string templateKey, DateTime utcNow)`

---

## 3. Application Layer (`Guts.Application`) — Use-Case Orchestration Only

> [!IMPORTANT]
> Zero domain logic allowed here. Services strictly coordinate repositories, domain factories/methods, project archive reading, token issuance, and DTO mapping.

### Security Extensions (`Guts.Application/Security/`)
- **`GutsClaimTypes`**: Add `public const string ExamGuest = "exam_guest";` and `public const string ExamId = "exam_id";`.
- **`ITokenAccessPassFactory` / `JwtSecurityTokenAccessPassFactory`**:
  - Add `TokenAccessPass CreateExamGuestToken(int examId, int expirationMinutes = 480);` generating a signed JWT containing `JwtRegisteredClaimNames.Jti`, `GutsClaimTypes.ExamGuest = "true"`, and `GutsClaimTypes.ExamId = examId.ToString()` (and **no** `NameId`, `Sub`, `Email`, or `Role` claims).

### Repository & Importer Contracts (`Guts.Application/Repositories/` & `Guts.Application/Services/Exam/`)
- **`IExamRepository`**:
  - `Task<Exam> GetByIdWithQuestionsAsync(int examId)` (throws `DataNotFoundException` if missing)
  - `Task<IReadOnlyList<Exam>> GetByCourseAndPeriodAsync(int courseId, int periodId)`
  - `Task<Exam> AddAsync(Exam exam)`
  - `Task UpdateAsync(Exam exam)`
  - `Task DeleteAsync(Exam exam)`
- **`IExamProjectImporter`** (interface in `Guts.Application/Services/Exam/`, implemented in `Guts.Infrastructure/Exam/`):
  - `Task<IReadOnlyList<ImportedProjectFileDto>> ExtractFromZipAsync(Stream zipStream, CancellationToken cancellationToken = default)`
  - `Task<IReadOnlyList<ImportedProjectFileDto>> CloneFromGitAsync(string repositoryUrl, string branchOrRef = null, CancellationToken cancellationToken = default)` *(sanitized HTTPS URL validation, shallow `--depth 1` clone to temp dir, immediate cleanup)*

### Application Service (`Guts.Application/Services/Exam/`)
- **Interface**: `IExamService`
- **Implementation**: `ExamService` (Scoped via `*Service` convention in `StartUpExtensions.AddApplicationLayerServices`)
  - **Teacher Authoring**:
    - `GetCourseExamsAsync(int courseId, int? periodId)`
    - `GetExamForLectorAsync(int examId)` (returns full detail including `AccessCode`)
    - `CreateExamAsync(...)` (uses `IExamFactory`, persists via `IExamRepository`)
    - `UpdateExamAsync(int examId, ...)` (loads aggregate ➔ `exam.UpdateMetadata(...)` ➔ persists)
    - `OpenExamAsync(int examId)` / `CloseExamAsync(int examId)` (loads aggregate ➔ `exam.Open(utcNow)` / `exam.Close(utcNow)` ➔ persists)
    - `DeleteExamAsync(int examId)`
    - `AddOrUpdateQuestionAsync(...)`, `DeleteQuestionAsync(...)`
    - `ImportQuestionFilesFromZipAsync(int examId, int questionId, Stream zipStream)` / `ImportQuestionFilesFromGitAsync(...)` (calls `IExamProjectImporter` ➔ passes `(ExamFilePath, Content, DefaultRole)` to `question.ReplaceFiles(...)` ➔ persists)
    - `UpdateQuestionFileRolesAsync(int examId, int questionId, IReadOnlyList<FileRoleAssignmentDto> updates)`
  - **Student Exam Entry & Workspace**:
    - `EnterExamAsync(int examId, string accessCode)`: loads `Exam` ➔ calls `exam.EnsureCanEnter(accessCode)` ➔ calls `tokenAccessPassFactory.CreateExamGuestToken(exam.Id)` ➔ returns `ExamEntryResultDto` (`TokenAccessPass` + `ExamWorkspaceDto`).
    - `GetExamWorkspaceAsync(int examId)`: loads `Exam` ➔ checks `exam.Status == ExamStatus.Open` (for guests) or allows `Lector`/`Admin` preview ➔ maps to `ExamWorkspaceDto` (never exposing `AccessCode`).

### DTOs & Converters (`Guts.Application/Dtos/Exam/` & `Guts.Application/Converters/`)
- `ExamSummaryDto`, `ExamDetailDto`, `ExamQuestionDto`, `ExamQuestionFileDto`, `ExamWorkspaceDto`, `ImportedProjectFileDto`, `ExamEntryResultDto`.
- **`IExamConverter` / `ExamConverter`** (Singleton via `*Converter` convention).

---

## 4. Infrastructure Layer (`Guts.Infrastructure`)

### EF Core Configuration & Migration
All EF Core types and configurations are `internal` per `ArchitectureTests.InfrastructureDbContextAndEntityConfigurations_ShouldBeInternal`:
- **`GutsContext`**: Add `public DbSet<Exam> Exams { get; set; }` (`ExamQuestions` and `ExamQuestionFiles` mapped via owned/child entity configurations).
- **Entity Configurations** (`Guts.Infrastructure/EntityConfigurations/`):
  - `ExamConfiguration` (`internal`): Table `"Exams"`, PK `Id`, `Title` (`MaxLength(200)`, required), `Description` (nvarchar(max)), `AccessCode` value conversion on `ExamAccessCode.Value` (`MaxLength(64)`, required), `TemplateKey` (`MaxLength(100)`), `Status` (`int`), FK to `Course` (`DeleteBehavior.Cascade`) and `Period` (`DeleteBehavior.Restrict`), index on `(CourseId, PeriodId)`.
  - `ExamQuestionConfiguration` (`internal`): Table `"ExamQuestions"`, PK `Id`, `Title` (`MaxLength(200)`), `InstructionsMarkdown`, `OrderIndex`, `TemplateKey` (`MaxLength(100)`), FK to `Exam` (`DeleteBehavior.Cascade`), index on `(ExamId, OrderIndex)`.
  - `ExamQuestionFileConfiguration` (`internal`): Table `"ExamQuestionFiles"`, PK `Id`, `RelativePath` value conversion on `ExamFilePath.Value` (`MaxLength(260)`), `Content` (nvarchar(max)), `Role` (`int`), unique index on `(ExamQuestionId, RelativePath)`, FK to `ExamQuestion` (`DeleteBehavior.Cascade`).
- **Migration Required**: `Yes` — `AddCodingExams`

### Repository & Importer Implementations (`Guts.Infrastructure/`)
- **`ExamDbRepository`** (`internal` or `public` implementing `IExamRepository`, auto-registered by `*Repository` convention):
  - Eager loads `Questions` (`OrderBy(q => q.OrderIndex)`) and `Files` (`OrderBy(f => f.RelativePath)`) using `.AsSplitQuery()` in `GetByIdWithQuestionsAsync`.
- **`ExamProjectImporter`** (`Guts.Infrastructure/Exam/ExamProjectImporter.cs` implementing `IExamProjectImporter`):
  - **Zip extraction**: Enforces max archive size (e.g., `10 MB`), max file count (`200`), max uncompressed file size (`1 MB`), skips binary/build directories (`bin/`, `obj/`, `.vs/`, `.git/`, `node_modules/`), skips binary files, auto-suggests `PublicTest` for files under `*Tests*` paths/projects, `ReadOnly` for `.sln`/`.csproj`/interfaces, and `Editable` for remaining `.cs` files.
  - **Git import**: Validates `https://` scheme (rejects `file://`, local paths, or private network SSRF targets), runs `git clone --depth 1 --single-branch` with timeout into an isolated temp directory, extracts allowed text files, and deletes the temp directory in `finally`.
  - Registered explicitly in `StartUpExtensions.AddInfrastructureLayerServices` (or named `ExamProjectImportRepository`/`Service`? Wait: `StartUpExtensions` registers `*Repository` in Infrastructure and `*Service` in Application, so explicitly register `services.AddScoped<IExamProjectImporter, ExamProjectImporter>();` in `StartUpExtensions.AddInfrastructureLayerServices`).

---

## 5. API Layer (`Guts.Api`) & Self-Hosted WASM Compiler Project

### Authorization Attribute & Lockdown Filter (`Guts.Api/Filters/`)
- **`AllowExamGuestAttribute`** (`[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]`): Marker attribute authorizing `ExamGuest` tokens on specific exam-guest endpoints.
- **`ExamGuestLockdownFilter`** (`IAsyncAuthorizationFilter`, registered globally in `Startup.ConfigureServices` inside `AddControllers(options => ...)`):
  - Inspects `context.HttpContext.User`: if `User.HasClaim(GutsClaimTypes.ExamGuest, "true")`:
    - If endpoint does **not** have `[AllowExamGuest]` metadata ➔ immediately sets `context.Result = new ForbidResult()` (`403 Forbidden`).
    - If route data contains `examId` and it does not equal the `exam_id` claim ➔ `403 Forbidden`.

### REST Endpoints (`Guts.Api/Controllers/`)
All controllers reside in `Guts.Api.Controllers`, end in `Controller`, and inherit from `Guts.Api.Controllers.ControllerBase` (satisfying `ArchitectureTests.ApiControllers_ShouldBeSecuredAndResideInControllersNamespace`):

1. **`ExamController`** (`[Route("api/exams")]`, `[Authorize(Policy = ApiConstants.LectorOrAdminPolicy)]`):
   - `GET /api/exams/templates` — returns curated `ExamTemplateCatalog` options.
   - `GET /api/exams/course/{courseId}?periodId={periodId}` — list exams for course/period (verifies course ownership for Lectors).
   - `GET /api/exams/{examId}` — get full exam details for Lector/Admin.
   - `POST /api/exams` — create `Draft` exam.
   - `PUT /api/exams/{examId}` — update exam metadata / `AccessCode`.
   - `POST /api/exams/{examId}/open` / `POST /api/exams/{examId}/close` — transition status.
   - `DELETE /api/exams/{examId}` — delete exam.
   - `POST /api/exams/{examId}/questions` / `PUT /api/exams/{examId}/questions/{questionId}` / `DELETE /api/exams/{examId}/questions/{questionId}`
   - `POST /api/exams/{examId}/questions/{questionId}/import-zip` (`IFormFile`) & `import-git` (`ImportGitRequestModel`)
   - `PUT /api/exams/{examId}/questions/{questionId}/files` — update file roles (`Editable` / `ReadOnly` / `PublicTest`) or starter content.
   - `GET /api/exams/{examId}/preview` — returns `ExamWorkspaceModel` for Lector dry-run preview without needing an `AccessCode`.

2. **`ExamSessionController`** (`[Route("api/exam-sessions")]`):
   - `POST /api/exam-sessions/{examId}/enter` (`[AllowAnonymous]`): accepts `EnterExamRequestModel { AccessCode }`; returns `200 OK` with `ExamEntryResponseModel { Token, Expiration, Workspace }` (or `403`/`400` on invalid code or non-`Open` exam).
   - `GET /api/exam-sessions/{examId}/workspace` (`[AllowExamGuest]`): returns `ExamWorkspaceModel` for the matching `exam_id` guest claim (used on page refresh/re-load while guest token is still cached in `sessionStorage`).

### Client-Side Compiler WASM Project (`Backoffice/Guts.ExamCompiler.Wasm/`)
- A standalone `.NET 10` WebAssembly browser project (`Microsoft.NET.Sdk.WebAssembly` / `WasmApp`) built during CI/publish and copied into `Guts.Web/ClientApp/src/assets/wasm-compiler/` (self-hosted, zero CDN requests, single-threaded Web Worker compatible with IIS without COOP/COEP headers).
- Exposes `[JSExport]` entrypoints called from a Web Worker (`roslyn-worker.ts`):
  - `Initialize(string templateKey, byte[][] referenceAssemblies)` — creates/caches Roslyn `MetadataReference`s per curated template.
  - `GetCompletions(...)`, `GetSignatureHelp(...)`, `GetHover(...)`, `GetDiagnostics(...)` — deterministic, zero-AI Roslyn `CSharpCompilation` + semantic model providers.
  - `BuildAndRun(string templateKey, ExamCompileFile[] files, bool runTests)` — emits in-memory PE assembly (`CSharpCompilation.Emit`), reports compiler diagnostics, and (for pure `.NET` templates) loads the assembly in an isolated collectible `AssemblyLoadContext` to execute `Program.Main` or discover/run `[Test]` / `[Fact]` methods with `TestContext` / stdout capture.

---

## 6. Frontend Layer (`Guts.Web/ClientApp`)

### Assets & Bundling (`angular.json` & `package.json`)
- Add npm dependencies: `monaco-editor`, `jszip` (zero external CDNs).
- Configure `angular.json` `assets` to copy `node_modules/monaco-editor/min/vs` to `assets/monaco/vs` and include `src/assets/wasm-compiler` (and curated reference DLLs under `src/assets/wasm-refs/{templateKey}/`).
- Configure `Guts.Web` static file caching (`Cache-Control`) for versioned `.wasm` / `.dll` assets so 100+ students load quickly from browser cache.

### Services & ViewModels (`src/app/services/`, `src/app/viewmodels/`)
- **`exam.model.ts`**: TypeScript interfaces for `ExamSummary`, `ExamDetail`, `ExamQuestion`, `ExamQuestionFile`, `ExamFileRole`, `ExamTemplateOption`, `ExamWorkspace`, `DiagnosticItem`, `TestExecutionResult`.
- **`ExamManagementService`** (`src/app/services/exam-management.service.ts`): Teacher CRUD, Zip/Git import, file-role tagging, Open/Close toggles, Lector preview fetch.
- **`ExamSessionService`** (`src/app/services/exam-session.service.ts`):
  - Holds the exam-scoped guest JWT in `sessionStorage` (`guts_exam_guest_token_{examId}`) so it never overwrites a Lector's `localStorage` session during preview and cannot access regular GUTS routes.
  - Calls `POST /api/exam-sessions/{examId}/enter` and `GET /api/exam-sessions/{examId}/workspace`.
- **`ExamDraftStorageService`** (`src/app/services/exam-draft-storage.service.ts`):
  - Primary storage in `IndexedDB` (`guts-exam-drafts` DB, `drafts` store keyed by `examId`), automatic fallback to `localStorage` (`guts_exam_draft_{examId}`) if `IndexedDB` is blocked/throws.
  - Stores `{ examId, updatedAtUtc, filesByQuestion: Record<number, Record<string, string>> }`.
  - Debounced save (`750ms`), `resetQuestionToStarter(...)`, `clearExamDraft(examId)`, and `purgeExpiredDrafts(maxAgeDays = 7)` on startup.
- **`RoslynWasmService`** (`src/app/services/roslyn-wasm.service.ts`):
  - Manages the `roslyn-worker.ts` Web Worker lifecycle, registers Monaco's `CompletionItemProvider`, `SignatureHelpProvider`, `HoverProvider` for `csharp`, publishes diagnostic markers per file model, and supports **`terminateAndRestart()`** if student code hits an infinite loop in WASM.
- **`ExamExportService`** (`src/app/services/exam-export.service.ts`):
  - `downloadQuestionZip(...)` & `downloadExamZip(...)` using `JSZip` + `file-saver` (merging `Editable` drafts with `ReadOnly` + `PublicTest` files, excluding `bin/`/`obj/`).
  - `formatFilesForClipboard(...)` generating `// === File: <relativePath> ===` formatted text for one-click **Copy Code** / **Copy All Editable Code**.

### Interceptors, Routing & Components
- **`TokenInterceptor` (`interceptors/tokeninterceptor.ts`)**:
  - When requesting `/api/exam-sessions/{examId}/workspace`, attaches the `ExamSessionService` guest token (if present) instead of regular `AuthService` token.
- **`AppComponent` (`components/app.component.*`)**:
  - Observes route data (`data: { examShell: true }`) to hide `<app-nav-menu>`, `<app-sidebar-menu>`, and impersonation chrome so `/exam/*` renders full-bleed in distraction-free Exam Mode.
- **Routes (`app-routing.module.ts`)**:
  - Teacher management inside course shell:
    - `/courses/:courseId/exams` ➔ `CourseExamsComponent` (guarded by `authGuard` + `lectorOrAdminGuard`)
    - `/courses/:courseId/exams/:examId/edit` ➔ `ExamEditorComponent` (authoring, Zip/Git import, file-role tagging, Open/Close, Preview button)
  - Student Exam Mode (no `authGuard`; uses Access Code entry):
    - `/exam/:examId` (`data: { examShell: true }`) ➔ `ExamShellComponent` (shows `ExamEntryCardComponent` if no workspace loaded yet, then switches to `ExamWorkspaceComponent` with question tabs, Markdown instructions, file explorer, Monaco Editor, Build/Run/Stop toolbar, output/test console, Reset, Clear Local Draft, Download `.zip`, and Copy Code buttons).
    - `/courses/:courseId/exams/:examId/preview` (`canActivate: [authGuard, lectorOrAdminGuard]`, `data: { examShell: true }`) ➔ `ExamShellComponent` in Lector preview mode.

---

## 7. Dependency Injection (`Guts.Bootstrapper`)

- **`ExamFactory`** (`Guts.Domain/ExamAggregate/`) implements `IExamFactory` ➔ auto-registered as `Singleton` (`*Factory` convention).
- **`ExamConverter`** (`Guts.Application/Converters/`) implements `IExamConverter` ➔ auto-registered as `Singleton` (`*Converter` convention).
- **`ExamService`** (`Guts.Application/Services/Exam/`) implements `IExamService` ➔ auto-registered as `Scoped` (`*Service` convention).
- **`ExamDbRepository`** (`Guts.Infrastructure/Repositories/`) implements `IExamRepository` ➔ auto-registered as `Scoped` (`*Repository` convention).
- **Explicit Registrations in `StartUpExtensions.AddInfrastructureLayerServices`**:
  - `services.AddScoped<IExamProjectImporter, ExamProjectImporter>();`
- **Explicit Registrations in `Guts.Api/Startup.cs`**:
  - Register `ExamGuestLockdownFilter` in `AddControllers(options => options.Filters.Add<ExamGuestLockdownFilter>())`.
