# Specification: Secure In-Browser Monaco Coding Exams

- **Feature ID**: `007-monaco-coding-exams`
- **Status**: `Approved`
- **Created**: `2026-11-17`

---

## 1. Context & Problem Statement

Traditional laptop programming exams using local IDEs (Visual Studio, Rider, IntelliJ) allow students to run offline AI models to generate solutions. To prevent unauthorized AI usage, PXL University of Applied Sciences and Arts runs proctored Blackboard exams via **Schoolyear**, which restricts student devices to explicitly whitelisted web origins/URLs (such as the GUTS website).

Allowing students to browse regular GUTS inside Schoolyear introduces two risks:
1. Students could browse their own previously submitted homework solutions, course materials, or documentation.
2. If student exam code were stored on the server behind a personal GUTS login, students could share their GUTS credentials to pull another student's in-progress solution.

This feature adds a **modular Exam bounded context inside the GUTS `Backoffice` monolith** (`Guts.slnx` + `Guts.Web/ClientApp`) with **zero server-side student code storage**:
1. **Access-Code-Only Student Entry & Exam Lockdown**: Students enter an open exam using *only* the teacher-issued `AccessCode` (no personal GUTS login required)—eliminating forgotten-password issues, credential-sharing attacks, and server-side student code leakage. The API issues an exam-scoped guest token locked exclusively to exam endpoints, and Schoolyear whitelists only the dedicated `/exam/*` route prefix so regular GUTS homework/courses are unreachable. Exam timing and deadlines remain handled by Blackboard.
2. **Existing Backoffice Auth for Lectors**: Lectors and Administrators use their standard GUTS Backoffice login (`LectorOrAdminPolicy`) to author, import, open, and close exams under their courses and periods.
3. **Self-Hosted Monaco + Deterministic Roslyn `.NET WASM` Worker**: Full, zero-AI IDE editing, IntelliSense, C# compilation (Console, Class Library, and ASP.NET Core reference assemblies), and pure `.NET` / sample test execution running 100% inside the student's browser sandbox—with zero external CDNs and zero server-side code execution.
4. **Browser-Only Draft Persistence (`IndexedDB` + `localStorage` fallback) & Blackboard Handoff**: Debounced local draft saving survives page refreshes and tab crashes within the same Schoolyear session without storing any student code on the server, paired with one-click **Download `.zip`** and **Copy Code** fallback for Blackboard submission.

Server-side automated grading (hidden tests / Docker grading runner) and extracting exams into a separate microservice are out of scope for v1, though keeping `ExamAggregate` cleanly isolated inside `Backoffice/` preserves easy future extraction.

---

## 2. User Stories & Actors

- **Actors**: `Student` | `Lector (Teacher)` | `Administrator`

- **US-1**: As a **Lector**, I want to create a coding exam for a `Course` and `Period` with an access code, import a standard `.NET` solution/project (via Zip or Git), tag files as `Editable`, `ReadOnly`, or `PublicTest` using curated templates, and open/close the exam manually so students get a realistic multi-file environment without a local IDE.
- **US-2**: As a **Lector**, I want exam access to use Access-Code-only entry, strict API/UI lockdown, and zero server-side student code storage so students cannot access prior GUTS homework or share solutions by trading GUTS logins.
- **US-3**: As a **Student**, I want to open the exam URL, enter the exam access code (without needing my personal GUTS password), write code in Monaco with deterministic Roslyn IntelliSense, and compile/run my code and sample tests in-browser without AI assistance.
- **US-4**: As a **Student**, I want my edits automatically persisted locally in the browser (`IndexedDB` / `localStorage`) so a page refresh or tab crash does not lose my work, and I want to reset to starter code if needed.
- **US-5**: As a **Student**, when finishing my exam work I want to download a `.zip` of my code (or copy all editable code as a fallback) to upload/paste into my Blackboard exam.
- **US-6**: As a **Lector**, I want the downloaded `.zip` to be a valid `.NET` project/solution structure so I can download student submissions from Blackboard, open them in my local IDE (Visual Studio / Rider), and run my grading tests on my own machine.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Exam Creation, Status & Access Code Management
- **Given** an authenticated `Lector` (owning the target `Course`) or `Administrator`
- **When** they create or update an `Exam` under a `Course` and `Period`
- **Then**:
  - The exam stores `Title`, `Description` (Markdown instructions), `CourseId`, `PeriodId`, `AccessCode`, `TemplateKey` (curated runtime/package profile), and `Status` (`Draft` | `Open` | `Closed`).
  - Only `Open` exams accept student exam entry.
  - A `Lector` can open a `Draft` exam, close an `Open` exam, reopen a `Closed` exam, or rotate the `AccessCode` at any time (actual exam timing/deadlines are managed by Blackboard).

### AC-2: Multi-File Question Authoring via Zip/Git Import & Curated Templates
- **Given** an `Exam` in `Draft` status
- **When** a `Lector` adds an `ExamQuestion` (`Title`, `InstructionsMarkdown`, `OrderIndex`, `TemplateKey`) and imports a `.NET` project/solution archive (`.zip`) or Git repository snapshot
- **Then**:
  - GUTS extracts source and project files into `ExamQuestionFile` records and allows the `Lector` to tag each file's role in the web UI:
    - `Editable`: starter code editable by the student during the exam.
    - `ReadOnly`: context files (interfaces, models, DTOs, `.csproj`, `.sln`) visible in Monaco and included in the exported `.zip`, but non-editable.
    - `PublicTest`: sample/smoke unit tests visible in Monaco, runnable in the student's browser WASM runtime, and included in the exported `.zip`.
  - Projects target a **curated set of pre-installed `.NET` / NuGet templates** (e.g., `.NET 10 Console`, `.NET 10 Class Library`, `NUnit` / `xUnit`, `ASP.NET Core Web API`, `EF Core InMemory`) so reference assemblies are pre-cached for browser WASM.
  - A `Lector` can preview/dry-run the question in the Exam Shell before opening the exam.

### AC-3: Access-Code-Only Exam Entry & Strict Exam-Mode Lockdown (API + Angular UI)
- **Given** an `Open` exam
- **When** a `Student` opens `/exam/{examId}` and submits the matching `AccessCode` (no personal GUTS username/password required)
- **Then**:
  - `Guts.Api` verifies `Status == Open` and the `AccessCode`, and issues an **exam-scoped guest JWT token** containing an explicit exam-lock claim (e.g., `exam_id` and `exam_guest = true`).
  - **API Lockdown**: Any request carrying an exam-scoped guest token to non-exam endpoints (`/api/courses/*`, `/api/assignments/*`, `/api/testruns/*`, `/api/gamification/*`, past `SolutionFile` endpoints, etc.) is rejected with **`403 Forbidden`** by an authorization policy/filter.
  - **UI Lockdown**: The Angular SPA enters a dedicated distraction-free Exam Shell (no top navigation bar, no course browser, no login/profile links, no past solutions) showing only the question selector, instructions, Monaco file tree/editor, compiler/test output panel, **Reset to Starter Code**, **Clear Local Draft**, **Download Code (.zip)**, and **Copy Code** actions.
  - Zero student code or identifying student exam answers are stored on the server.

### AC-4: Self-Hosted Monaco Editor with Deterministic Roslyn WASM IntelliSense
- **Given** a `Student` actively working inside the Exam Shell
- **When** the exam workspace loads in the browser
- **Then**:
  - All Monaco Editor scripts, web workers, `.NET WebAssembly` runtime files, and curated `.NET` / ASP.NET Core reference assemblies are **100% self-hosted** by `Guts.Web` (zero external CDN requests, ensuring Schoolyear compatibility).
  - The `.NET WASM` runtime runs in a single-threaded browser Web Worker (avoiding `SharedArrayBuffer` / COOP-COEP header constraints on IIS).
  - Monaco connects to the Roslyn WASM worker to provide deterministic, **zero-AI** IDE assistance:
    - Type-aware autocomplete (`CompletionItemProvider`)
    - Parameter signature help (`SignatureHelpProvider`)
    - Hover type/doc tooltips (`HoverProvider`)
    - Live compiler diagnostics (red squiggles and error list) across all `Editable`, `ReadOnly`, and `PublicTest` files in the workspace.
  - `ReadOnly` and `PublicTest` files open in Monaco read-only mode; only `Editable` files can be modified.

### AC-5: In-Browser Compilation & Execution of Pure .NET / Tests
- **Given** a `Student` editing an `ExamQuestion` in Monaco
- **When** the student clicks **Build** or **Run / Run Tests**
- **Then**:
  - The Roslyn WASM worker compiles all workspace files (`Editable` + `ReadOnly` + `PublicTest`) against the question's curated reference DLLs (including ASP.NET Core assemblies for compile-time checking).
  - For pure `.NET` exercises (Console / Class Library / pure unit tests), the WASM worker executes `Program.Main` or the `PublicTest` suite in-browser and displays stdout, pass/fail status, assertion messages, and stack traces.
  - For ASP.NET Core exercises requiring a live HTTP server or `WebApplicationFactory`, compilation diagnostics run in-browser while runtime HTTP execution displays a clear notice that live server hosting is unavailable in the browser sandbox.
  - Zero student code is sent to, compiled on, or executed on the `Guts.Api` server.

### AC-6: Browser-Only Draft Persistence (`IndexedDB` + `localStorage` Fallback) & Cleanup
- **Given** an active exam workspace in the student's browser
- **When** the `Student` edits `Editable` files, refreshes the page, recovers from a tab crash, or clicks **Reset to Starter Code** / **Clear Local Draft**
- **Then**:
  - Edits to `Editable` files are debounced (e.g., `500–1000 ms` after typing) and persisted locally in `IndexedDB` (with automatic fallback to `localStorage` if `IndexedDB` is unavailable) keyed by `examId` (with `updatedAtUtc` metadata).
  - Refreshing the page or recovering from a tab crash within the same Schoolyear browser session automatically restores the student's latest local edits into Monaco.
  - Clicking **Reset to Starter Code** (after confirmation) restores the current question (or file) to its original `Editable` starter content and updates local storage; clicking **Clear Local Draft** removes the local draft for that exam from `IndexedDB`/`localStorage`.
  - On app/exam startup, stale local drafts older than a configured TTL (e.g., `7 days`) are automatically purged from `IndexedDB` / `localStorage` so old code does not linger on shared machines.

### AC-7: Client-Side `.zip` Export & Copy-Paste Fallback for Blackboard
- **Given** a `Student` working on or finishing an `Exam` in the Exam Shell
- **When** the student clicks **Download Code (.zip)** (per question or entire exam) or **Copy Code** (active file or all `Editable` files formatted with file-path headers)
- **Then**:
  - **Primary (`.zip` export)**: A `.zip` archive is generated directly in the browser containing the complete project/solution structure: the student's current `Editable` files merged with the question's `ReadOnly` context files (`.csproj`, `.sln`, models, interfaces) and `PublicTest` files (excluding `bin/` or `obj/`), ready to upload to Blackboard and open cleanly in Visual Studio, Rider, or VS Code (`dotnet test`).
  - **Fallback (Copy/Paste)**: If `.zip` downloads/uploads are restricted in Schoolyear, the student can use Monaco's standard copy/paste or the one-click **Copy All Editable Code** action (copying all modified files with clear `// === File: <path> ===` delimiters) and paste their solution directly into the Blackboard exam answer box.

---

## 4. Domain Rules & Invariants

- **DR-1 (`Exam` Aggregate)**:
  - `Title` is required and non-empty; `CourseId > 0`, `PeriodId > 0`.
  - `AccessCode` must be non-empty before transitioning `Draft -> Open`.
  - An `Exam` cannot transition to `Open` unless it contains at least one `ExamQuestion`, and every `ExamQuestion` contains at least one `Editable` file and a valid `TemplateKey`.
  - Valid status transitions: `Draft -> Open`, `Open -> Closed`, `Closed -> Open`.
- **DR-2 (`ExamQuestion` & `ExamQuestionFile`)**:
  - File paths within a question must be unique (normalized forward-slash relative paths, no `..` path traversal).
  - Supported file roles in v1 are `Editable`, `ReadOnly`, and `PublicTest`.
- **DR-3 (`ExamAccessPolicy` / Entry Validation)**:
  - Entry is permitted **only** when `Exam.Status == Open` and the supplied `AccessCode` matches (constant-time / normalized comparison).
- **DR-4 (Zero Server-Side Student Code)**:
  - The backend domain and persistence model contain **no** entities, tables, or endpoints for storing student-submitted exam source code.

---

## 5. Authorization & Security Rules

- **Students**:
  - Do **not** log in with personal GUTS credentials; enter solely via `POST /api/exams/{examId}/enter` with the `AccessCode` while `Exam.Status == Open`.
  - Receive an exam-scoped guest token valid only for `GET /api/exams/{examId}/workspace`.
  - Exam-scoped guest tokens are rejected with **`403 Forbidden`** on all regular GUTS endpoints (`/api/courses/*`, `/api/assignments/*`, `/api/testruns/*`, `/api/users/*`, `/api/gamification/*`, etc.).
  - Cannot list `Draft` or `Closed` exams, or call any management endpoint (`401`/`403`).
- **Lectors**:
  - Users satisfying `LectorsOnlyPolicy` or `LectorOrAdminPolicy` (see [ApiConstants.cs](file:///c:/Workspace/PXL-IT-NET/Guts/Backoffice/Guts.Api/ApiConstants.cs)) who are assigned to the exam's `Course` may author exams, import projects, configure file roles, preview the Exam Shell, and open/close exams.
  - Lectors not assigned to that course receive **`403 Forbidden`**.
- **Administrators**:
  - Satisfy `LectorOrAdminPolicy` / `AdminOnlyPolicy` with full management access across courses.
- **Unauthenticated**:
  - Receive **`401 Unauthorized`**.
- **Schoolyear Route Isolation & Zero Server Code Execution**:
  - Schoolyear should whitelist the exam route prefix (e.g. `https://guts-web.pxl.be/exam/*`) so regular GUTS routes (`/login`, `/courses`, etc.) are blocked by Schoolyear.
  - Student code is compiled and executed strictly inside the browser WebAssembly sandbox; `Guts.Api` never compiles or executes student code.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Wrong `AccessCode` or exam not `Open` | Reject exam entry without issuing exam token | `403 Forbidden` / `400 Bad Request` |
| Exam-scoped guest token calls `/api/courses` or `/api/testruns` | Blocked by exam-lockdown authorization filter | `403 Forbidden` |
| Student refreshes browser or recovers from tab crash in Schoolyear | Re-entering (or cached guest token) loads starter files and `IndexedDB`/`localStorage` automatically restores local edits | `200 OK` |
| Student fully exits Schoolyear (wiping ephemeral browser profile) or switches to a loaner laptop | Browser storage is gone (known boundary of zero-server-code design); student starts fresh unless they already saved a `.zip` | N/A |
| `IndexedDB` disabled or blocked in kiosk browser | Service transparently falls back to `localStorage` | N/A |
| Lector closes exam (`Open -> Closed`) while a student already has it loaded | New entries are rejected; student's browser still allows exporting `.zip` or copying already-written local code for Blackboard | `403 Forbidden` on new entry |
| Schoolyear blocks `.zip` file download or Blackboard file upload | Student uses one-click **Copy Code** / **Copy All Editable Code** (or standard Monaco copy/paste) to paste code directly into Blackboard | N/A (client fallback) |
| Student code contains infinite loop or memory bomb in WASM | Student can cancel/terminate and restart the Web Worker without affecting the server or losing `IndexedDB`/`localStorage` drafts | N/A (browser-isolated) |
| Imported `.zip` contains path traversal (`../`) or unsupported NuGet dependencies | Rejected during import validation (`ContractException`) with clear diagnostic message | `400 Bad Request` |
| `Exam` not found | Throws `DataNotFoundException` | `404 Not Found` |

---

## 7. Non-Functional & Quality Gate Requirements

- **Test Coverage**: Unit and/or integration tests must cover as many lines and branches of all new and modified code as possible across every touched layer (`.NET` domain, application, infrastructure, API controllers/filters, and Angular components, services, guards, storage adapters, `.zip` builder, and WASM/Monaco adapters), satisfying the SonarCloud new-code coverage Quality Gate (`>= 80%`, targeting maximum achievable coverage).
- **Playwright E2E Coverage**: New UI flows (`AC-1`–`AC-7`: teacher exam authoring/tagging, Access-Code exam entry, API/UI Exam Mode lockdown guard, Monaco editing/in-browser build & test, `IndexedDB`/`localStorage` refresh recovery, reset to starter code, `.zip` download, and copy-code fallback) must have Playwright E2E test scenarios in `Backoffice/Guts.Web/ClientApp/e2e/tests/`.
- **Static Analysis & Security**: SonarCloud / SonarAnalyzer checks must pass with `0` open issues, `0` unreviewed security hotspots, and `0` ESLint errors.
- **Schoolyear Network Compatibility**: Zero runtime requests to external CDNs (`unpkg`, `jsdelivr`, `cdnjs`, Google Fonts, etc.); all Monaco, `JSZip` (or client zip utility), and `.NET WASM` assets served locally from `Guts.Web`.
- **Performance**: Initial WASM worker + reference assembly bundle should be browser-cached (`Cache-Control` / versioned static assets) so exam start across a classroom of `100+` concurrent students loads quickly without saturating `Guts.Web`.

---

## 8. Out of Scope

- Exam scheduling, countdown timers, and automatic submission deadlines in GUTS (handled by Blackboard).
- Extracting the Exam module into a separate microservice in v1 (kept as a modular `ExamAggregate` bounded context inside `Backoffice/` for now, structured to allow future extraction).
- Storing student exam code or submissions on the server (by design, to prevent credential-sharing solution theft).
- Automated server-side grading, `HiddenTest`s, Docker grading workers, and grading dashboards (student `.zip`/pasted submissions are uploaded to and graded via Blackboard + the teacher's local IDE).
- Java (`JUnit 5`) in-browser compilation and test execution in v1.
- Live HTTP server execution (Kestrel) inside browser WebAssembly during the exam (ASP.NET Core projects compile with IntelliSense in WASM; full integration tests are run by the teacher locally after downloading the `.zip` from Blackboard).
- Direct Blackboard LTI 1.3 authentication or automated Blackboard Gradebook passback.
- AI code completion or AI assistance.
- Cross-device recovery if Schoolyear wipes its ephemeral profile or the student switches laptops.

---

## 9. Open Questions

- [ ] **Q1**: Verify in a Schoolyear test session whether `.zip` download/upload and `/exam/*` URL prefix whitelisting work cleanly between GUTS and Blackboard, or if students will rely on the clipboard copy-paste fallback (`AC-7`). — *Decision: Pending Schoolyear test*
- [ ] **Q2**: Which exact curated `.NET 10` template profiles and NuGet packages are needed for the first pilot exam (e.g., `NUnit` vs `xUnit`, `Moq` / `FluentAssertions`, `Microsoft.EntityFrameworkCore.InMemory`)? — *Decision: Pending*
- [x] **Q3**: Host as a separate microservice or inside the existing `Backoffice/` monolith? — *Decision: Keep inside the `Backoffice/` monolith as a clean modular bounded context for v1.*
