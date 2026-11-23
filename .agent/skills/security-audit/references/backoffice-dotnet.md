# Security Audit Checklist — Backoffice .NET API, DDD Layers & MCP Server

Use this checklist when auditing `Backoffice/` backend projects (`Guts.Api`, `Guts.Application`, `Guts.Domain`, `Guts.Infrastructure`, `Guts.Bootstrapper`, `Guts.Common`, `Guts.Architecture.Tests`).

---

## 1. Access Control, RBAC & Multi-Tenancy (OWASP A01 / API1 / API5 / CWE-284, CWE-639, CWE-862)

GUTS defines roles (`Student`, `Lector`, `Admin`) and authorization policies in `Backoffice/Guts.Api/Startup.cs` (`AuthenticatedUsersOnlyPolicy`, `LectorsOnlyPolicy`, `AdminOnlyPolicy`, `LectorOrAdminPolicy`, `QuartzDashboardAdminPolicy`) plus a global `AuthorizeFilter` and `ImpersonationReadOnlyFilter`.

- **BOLA / IDOR (API1 / CWE-639)**:
  - For every endpoint taking `userId`, `teamId`, `assignmentId`, `testRunId`, `solutionFileId`, `assessmentId`, or `jobId`: verify ownership or team membership is enforced for `Student` callers (e.g., `IsOwnUserId(userId)`, team membership check, or restricting to `Lector`/`Admin`).
  - Verify students cannot read other students' `SolutionFile` source code, test runs, peer assessment submissions, or exam scores by guessing sequential integer IDs.
  - Verify peer assessment endpoints (`ProjectAssessmentController`, `ProjectTeamAssessmentController`) prevent a student from reading or submitting evaluations for a team they do not belong to, or reading raw individual peer scores meant to be anonymous/aggregated.
- **BFLA & Privilege Escalation (API5 / CWE-862)**:
  - Audit every `[AllowAnonymous]` controller/action (`AuthController`, `HomeController`, etc.) to confirm anonymous access is strictly necessary and cannot leak internal data or mutate privileged state.
  - Verify admin/lector endpoints (`AdminUserController`, `ImpersonationController`, `ScheduledJobsController`, `DeletionJobController`, `CourseController`, `PeriodController`, `ChapterController`, `ProjectController`) explicitly require `AdminOnlyPolicy`, `LectorsOnlyPolicy`, or `LectorOrAdminPolicy` — never rely solely on the default `AuthenticatedUsersOnlyPolicy`.
  - Check **Quartz Dashboard** (`MapQuartzDashboard().RequireAuthorization(ApiConstants.QuartzDashboardAdminPolicy)`) and `QuartzDashboardTicketStore` for single-use ticket expiration, constant-time/CSPRNG ticket generation, cookie `Secure`/`HttpOnly`/`SameSite`, and CSRF protection.
- **Impersonation Safety**:
  - Verify `ImpersonationController` restricts who can impersonate whom (e.g., prevent escalating to another `Admin`, prevent chaining impersonation, audit-log every impersonation start/stop).
  - Verify `ImpersonationReadOnlyFilter` cannot be bypassed by:
    - GET endpoints that perform state mutations.
    - Non-MVC endpoints (e.g., `/mcp`, Quartz Dashboard, minimal APIs).
    - HTTP method override headers (`X-HTTP-Method-Override`).
- **Mass Assignment / BOPLA (API3 / CWE-915)**:
  - Verify controllers bind dedicated `*Model` DTOs (never EF Core entities or domain aggregates directly).
  - Check `UserController` / `AuthController` / `AdminUserController` to ensure users cannot self-assign roles, `EmailConfirmed`, `LectorVerified`, or other privileged properties.

---

## 2. Authentication, JWT & Session Management (OWASP A07 / API2 / CWE-287, CWE-347, CWE-384)

Inspect `Startup.cs`, `AuthController.cs`, and token/identity services in `Guts.Application` / `Guts.Infrastructure`:

- **JWT Configuration (`Startup.cs`)**:
  - Verify `TokenValidationParameters` explicitly validates `ValidateIssuer = true`, `ValidateAudience = true`, `ValidateLifetime = true`, `ValidateIssuerSigningKey = true`, tight `ClockSkew`, and explicit `ValidAlgorithms` (reject `alg: none` and algorithm confusion).
  - Check `options.RequireHttpsMetadata` (flag `false` outside Development unless justified behind TLS-terminating reverse proxy with strict validation).
  - Check signing key entropy (`Tokens:Key` length >= 256 bits for HS256) and ensure no real secret is committed in `appsettings*.json` or `docker-compose*.yml`.
  - Audit `options.SaveToken = true` (unnecessary token retention in `AuthenticationProperties`).
- **Auth Flows (`AuthController.cs`)**:
  - **Brute Force & Account Enumeration (CWE-307, CWE-204)**: Check login, forgot-password, reset-password, email confirmation, and lector verification endpoints for lockout policies, rate limiting, reCAPTCHA fail-closed verification (don't bypass if reCAPTCHA service errors!), and uniform error responses that avoid leaking whether an email exists.
  - **Password Policy & Hashing**: Verify ASP.NET Identity password complexity, PBKDF2 iterations, and secure password reset token lifetime/single-use invalidation.
  - **Lector Verification**: Verify how lector role elevation works (`VerifyLectorModel`, `ResendLectorVerificationModel`) — ensure email domain checks cannot be bypassed (e.g., `evil-pxl.be` vs `@pxl.be`) and verification tokens are cryptographically random and short-lived.
- **Cookie Security**:
  - Inspect `CookiePolicyOptions` (`MinimumSameSitePolicy = SameSiteMode.None` is overly permissive unless strictly required for cross-site cookies) and `QuartzDashboardCookieScheme` (`SecurePolicy = CookieSecurePolicy.Always` in non-dev).

---

## 3. GUTS Anti-Tampering & Business Logic Integrity (OWASP A04, A08 / API6 / CWE-345, CWE-840)

GUTS's core academic integrity guarantee relies on test code hashing and server-side validation:

- **Test Code Hash Verification**:
  - Verify students can **never** register or overwrite teacher `TestCodeHash` values (strictly gated on verified `Lector`/`Admin` role).
  - Check cryptographic hash algorithm (SHA-256 or stronger — flag MD5/SHA-1 if used for integrity/tamper resistance) and constant-time comparison where applicable.
  - Verify test runs (`TestRunController`, `TestRunService`, `Assignment`) reject mismatched/empty hashes and cannot attach test results to another student's or team's assignment.
  - Check race conditions or replay attacks in test result submission and gamification XP/streak calculations.
- **Domain Invariants (DDD Alignment)**:
  - Verify security-critical business rules (ownership, hash validation, team membership, assessment deadlines, immutability of completed assessments) are enforced inside `Guts.Domain` via `Contracts.Require(...)`, not only in controllers or frontend checks.

---

## 4. Input Validation, Injection & File Handling (OWASP A03 / CWE-89, CWE-22, CWE-79, CWE-434)

- **SQL Injection (CWE-89)**:
  - Audit `Guts.Infrastructure` for `FromSqlRaw`, `ExecuteSqlRaw`, `FromSqlInterpolated` with string concatenation, or dynamic `OrderBy` / raw `DbCommand`. All queries must be parameterized.
- **SolutionFile & Export Handling (CWE-22, CWE-434, CWE-1236)**:
  - Validate `SolutionFile.FilePath` (using a `FilePath` Value Object / `Contracts.Require`) to reject path traversal (`..`, absolute paths, null bytes, invalid characters).
  - Check request body size limits (`[RequestSizeLimit]`) and file count/size caps on test run / solution file uploads to prevent storage/memory DoS (API4 / CWE-770).
  - Verify downloads (`Content-Disposition`) sanitize filenames against header injection (`\r`, `\n`, `"`) and serve student code with safe content types (`text/plain` or `application/octet-stream` + `X-Content-Type-Options: nosniff`) so uploaded HTML/SVG/JS cannot execute in the API origin!
  - Check CSV/Excel exports for formula injection (`=`, `+`, `-`, `@` prefix neutralization).
- **SSRF (OWASP A10 / API7 / CWE-918)**:
  - Audit all outbound `HttpClient` calls (e.g., reCAPTCHA verification, mail/webhook adapters) to ensure URLs are fixed/allowlisted and not user-controlled.
- **Log Injection & Sensitive Data Exposure (OWASP A09 / CWE-117, CWE-532)**:
  - Check Serilog / `ILogger` calls and `LogBadRequestFilterAttribute` to ensure passwords, JWTs, reset tokens, and raw `Authorization` headers are never logged, and user input is structured (not interpolated with unescaped CRLF).

---

## 5. MCP Server Security (`/mcp`, `Backoffice/Guts.Api/Mcp/`) (OWASP LLM / MCP Security)

Inspect `Startup.cs` (`AddMcpServer()`, `MapMcp("/mcp")`) and `Backoffice/Guts.Api/Mcp/`:

- **Endpoint Authorization & Exposure**:
  - Verify whether `endpoints.MapMcp("/mcp")` requires authentication/authorization or is intentionally public read-only documentation. Document the exact exposure and check for DoS/rate limiting.
  - If any MCP tools/prompts/resources accept user parameters or touch database/filesystem state: enforce strict authorization, input validation, and read-only guarantees.
- **Resource & Path Safety (`AttributeGuideService.cs`, `AttributeGuideResources.cs`)**:
  - Verify guide lookups use an explicit allowlist or embedded resources rather than constructing filesystem paths from MCP URI parameters (CWE-22).
- **Prompt Injection & Output Safety**:
  - Ensure MCP resources/prompts do not interpolate untrusted student-submitted content (solution code, team names, user profiles) into prompt templates without clear delimiting/escaping.

---

## 6. Security Misconfiguration, HTTP Hardening & DoS (OWASP A05 / API4 / API8 / CWE-16, CWE-942)

Inspect `Startup.cs`, `Program.cs`, `ApplicationExceptionFilterAttribute.cs`, and `appsettings*.json`:

- **Swagger / OpenAPI Exposure**: `UseOpenApi()` and `UseSwaggerUi()` are currently mounted unconditionally in `Startup.cs` — verify whether Swagger should be restricted in Production or intentionally public (note `backoffice-production.yml` currently probes `/swagger/index.html`).
- **CORS (`Startup.cs`)**: Verify `AllowedOrigins` never uses `*` with credentials, and check `AllowAnyHeader()` / `AllowAnyMethod()` necessity.
- **Forwarded Headers (`UseForwardedHeaders`)**: ASP.NET Core defaults `KnownProxies` to loopback only; verify proxy configuration cannot be spoofed via `X-Forwarded-For` / `X-Forwarded-Proto` (which affects IP logging, rate limiting, HSTS, and cookie `SecurePolicy = SameAsRequest`).
- **Security Response Headers**: Check `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY` / CSP `frame-ancestors 'none'`, `Referrer-Policy`, `Strict-Transport-Security` (HSTS `max-age` + `includeSubDomains`), and suppression of `Server` / `X-Powered-By` headers.
- **Rate Limiting & Resource Limits (API4 / CWE-770)**:
  - Check ASP.NET Core Rate Limiting middleware (`AddRateLimiter`) on auth endpoints (`/api/Auth/*`), deletion jobs (`DeletionJobController` / `InMemoryDeletionJobQueue` bounded capacity!), and large query/export endpoints.
  - Check pagination bounds (`pageSize`, `take`) on list endpoints.
- **Error Handling (`ApplicationExceptionFilterAttribute.cs`)**:
  - Verify stack traces, SQL errors, and internal file paths are never returned to clients outside Development (`env.IsDevelopment()`).

---

## 7. Dependencies & Architecture Guardrails (OWASP A06)

- Run `dotnet list Backoffice/Guts.slnx package --vulnerable --include-transitive`.
- Check `Backoffice/Guts.Architecture.Tests/ArchitectureTests.cs`:
  - `ApiControllers_ShouldBeSecuredAndResideInControllersNamespace` currently passes if a controller simply inherits `Guts.Api.Controllers.ControllerBase` — verify whether any controller/action can slip through without an explicit role policy where expected, and extend `ArchitectureTests.cs` for systemic findings fixed in Stage 2.
