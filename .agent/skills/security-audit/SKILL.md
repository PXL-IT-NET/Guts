---
name: security-audit
description: >-
  OWASP-based security audit, vulnerability scanning, and TDD remediation skill for all GUTS applications
  (Backoffice .NET 10 API & MCP server, Angular SPA & Guts.Web host, DotNetClient, JavaClients, and Docker/CI).
  Activate whenever auditing security, scanning for vulnerabilities, reviewing PRs/diffs for security issues,
  hardening authentication/authorization/anti-tampering/dependencies, or fixing security findings.
---

# GUTS — Security Audit & TDD Remediation Skill

Use this skill to scan, triage, and remediate security vulnerabilities across the GUTS repository using **OWASP Top 10**, **OWASP API Security Top 10**, **OWASP ASVS**, **CWE**, and the **GUTS-specific threat model** (anti-tampering test code hashing, student/lector/admin RBAC & impersonation, client token caching & solution file extraction, MCP endpoint security, and Docker/CI supply chain).

---

## Scope Selection & Progressive Reference Loading

Determine the requested audit scope first, then read **only** the relevant reference checklists from `./references/`:

| Target Scope | Trigger Examples | Reference File(s) to Load | `-Scope` for Pre-Scan |
| :--- | :--- | :--- | :--- |
| **Backoffice .NET API & MCP** | Controllers, Auth, JWT, EF Core, DDD, Quartz, `/mcp` | [backoffice-dotnet.md](./references/backoffice-dotnet.md) | `BackofficeDotNet` |
| **Backoffice Angular SPA & Web** | `Guts.Web`, `ClientApp`, interceptors, guards, XSS, headers | [backoffice-angular.md](./references/backoffice-angular.md) | `BackofficeAngular` |
| **Backoffice (Full)** | Entire `Backoffice/` | Both `backoffice-dotnet.md` and `backoffice-angular.md` | `Backoffice` |
| **.NET Client Libraries** | `DotNetClient/` (`Core`, `NUnit`, `XUnit`, `WPF`) | [dotnet-client.md](./references/dotnet-client.md) | `DotNetClient` |
| **Java Client Libraries** | `JavaClients/` (`guts-client-core`, `junit-5`) | [java-clients.md](./references/java-clients.md) | `JavaClients` |
| **DevOps, Containers & CI/CD** | Dockerfiles, `docker-compose*.yml`, `.github/workflows/`, `deploy/`, secrets | [devops-supply-chain.md](./references/devops-supply-chain.md) | `DevOps` |
| **Git Diff / PR Review** | Recent changes, staged/branch diff | Load references matching changed paths | `Diff` |
| **Full GUTS Repository** | "Audit the whole repo" / "Security review of all apps" | All 5 reference files | `All` |

---

## Two-Stage Interactive Workflow

Always follow this **two-stage workflow** unless the user explicitly asks to skip Stage 1 approval and fix immediately.

### Stage 1: Automated Pre-Scan, Manual Audit & Report (No Code Edits)

#### Step 1: Run Automated Pre-Scan & Native Security Checks
1. Run the zero-install pre-scan helper script from the repository root:
   ```powershell
   pwsh -File .agent/skills/security-audit/scripts/security-prescan.ps1 -Scope <Scope>
   ```
   - Add `-SkipNetworkChecks` if running offline/without registry access; note any skipped SCA checks in the report.
   - Add `-IncludeExternalTools` if `trivy`, `gitleaks`, `semgrep`, or `osv-scanner` are installed locally and you want deeper SCA/SAST/secret checks.
2. Run relevant native security/architecture checks for the scope:
   - **Backoffice**: `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj` and, when analyzing full Backoffice quality/hotspots, `Backoffice/run-sonarqube.ps1 -UseSonarCloud`.
   - **DotNetClient**: `dotnet list DotNetClient/Guts.sln package --vulnerable --include-transitive`.
   - **JavaClients**: inspect `JavaClients/**/build.gradle` and `gradle/wrapper/gradle-wrapper.properties`.

#### Step 2: Deep Manual Code Audit Against Reference Checklists
1. Read the routed checklist file(s) from `./references/` and inspect the actual source code systematically — **never rely solely on regex hits**.
2. Trace data flows end-to-end across trust boundaries:
   - Unauthenticated / Student / Lector / Admin / Impersonating-Admin roles
   - Client IDE -> Backoffice API -> Database -> Angular SPA rendering
   - Student-controlled inputs (source files, file paths, test names, markdown/HTML, team names, query parameters, JWT/cookie headers)
3. Audit existing security suppressions (e.g., `[SuppressMessage("Security", ...)]`, `#pragma warning disable S*`, `eslint-disable`) to verify their justifications are still accurate and narrowly scoped.

#### Step 3: Evidence-Based Triage & False-Positive Filtering
Before reporting any finding:
- **Verify exploitability & reachability** in actual code paths (check global filters like `AuthorizeFilter` and `ImpersonationReadOnlyFilter` in `Backoffice/Guts.Api/Startup.cs`, ASP.NET Core default protections, and Angular built-in sanitization).
- **Eliminate noise**: Do not report theoretical issues that are already mitigated by framework defaults or verified upstream controls.
- **Assign Severity** using consistent criteria:
  - **Critical**: Unauthenticated RCE, auth bypass, privilege escalation to Admin/Lector, mass PII/credential exposure, anti-tampering bypass affecting exam integrity.
  - **High**: IDOR/BOLA across students/teams, stored XSS, SQL/command/path injection, token theft, high-CVSS reachable dependency vulnerability, secret exposure in repo.
  - **Medium**: Missing defense-in-depth headers (CSP, HSTS, X-Frame-Options), CSRF/SameSite weaknesses, missing rate limiting on auth/expensive endpoints, Swagger/diagnostics exposed in production, insecure local token permissions, info disclosure.
  - **Low**: Minor hardening gaps, non-exploitable verbose errors, outdated transitive dev dependencies, CI workflow best-practice tightening.
- Assign each finding a stable ID (`SEC-001`, `SEC-002`, ...) mapped to **OWASP Top 10 / API Top 10**, **CWE**, and **ASVS** where applicable.

#### Step 4: Deliver Ephemeral Audit Report & Pause for Approval
- Read [audit-report-template.md](./resources/audit-report-template.md).
- Write the completed report as an **ephemeral conversation artifact** (in the conversation artifact directory) — **NEVER commit unpatched vulnerability reports or exploit details into the Git repository** (students and external users may read the repo).
- Present a concise summary in chat linking to the artifact and **pause for user approval** on which `SEC-*` findings to fix (and whether any large architectural hardening should become a `Backoffice/specs/` SDD spec first).

---

### Stage 2: Approved TDD Remediation & Systemic Guardrails

Once the user approves the findings to remediate, fix them in descending severity order (Critical -> High -> Medium -> Low):

1. **Write a Failing Security Regression / Abuse-Case Test First (TDD)**:
   - Model the attacker's behavior explicitly (e.g., Student A accessing Student B's data returns `403`/`404`, impersonating admin issuing `POST`/`PUT`/`DELETE` is blocked, tampered `TestCodeHash` is rejected, unauthenticated `/mcp` request is denied, XSS payload is neutralized, path traversal `../../.env` is rejected by `DotNetClient`/`JavaClients`).
   - Run the targeted test and confirm it **fails** before implementing the fix.
2. **Implement the Fix Idiomatically**:
   - **Backoffice**: Respect strict DDD layer boundaries (`Backoffice/AGENTS.md`) — put domain security invariants (e.g., ownership, hash integrity, file path validation) in `Guts.Domain` with `Contracts.Require(...)`; keep `Guts.Application` strictly orchestration; enforce auth policies, input bounds, headers, and rate limits in `Guts.Api` / `Guts.Web`.
   - **Angular SPA**: Fix client-side sinks, token handling, guards, and interceptors, and ensure backend enforces the matching control (never rely on client guards alone).
   - **DotNetClient & JavaClients**: Preserve public test-runner API compatibility unless breaking changes were explicitly approved in Stage 1.
   - **DevOps / CI**: Keep staging/production parity and avoid breaking IIS or Docker deployment flows.
3. **Add Permanent Systemic Guardrails (When Applicable)**:
   - If a finding represents a repeatable class of vulnerability, add an automated guardrail so it cannot regress:
     - Backend structural rules in `Backoffice/Guts.Architecture.Tests/ArchitectureTests.cs` (e.g., endpoint authorization, forbidden APIs, immutable security types).
     - Compile-time Roslyn/Sonar analyzer severity in `Backoffice/.editorconfig` or `Backoffice/Directory.Build.props`.
     - Frontend lint rules in `Backoffice/Guts.Web/ClientApp/.eslintrc*` or `eslint.config.*`.
4. **Full Verification & Quality Gates**:
   - Run all affected test suites:
     - Backoffice backend: `dotnet test Backoffice/Guts.slnx` (includes `Guts.Architecture.Tests`).
     - Backoffice frontend: `npm run lint`, `npm test -- --watch=false`, `npm run build`, and `npm run test:e2e` (mandatory whenever auth, guards, interceptors, or UI workflows change).
     - DotNetClient: `dotnet test DotNetClient/Guts.Client.Core.Tests/Guts.Client.Core.Tests.csproj` and `dotnet test DotNetClient/Guts.Client.NUnit.Tests/Guts.Client.NUnit.Tests.csproj` (plus `dotnet build DotNetClient/Guts.sln`).
     - JavaClients: `./gradlew test` (from `JavaClients/`).
   - Run `Backoffice/run-sonarqube.ps1 -UseSonarCloud` for Backoffice changes and verify `0` open vulnerabilities/hotspots and maximum new-code coverage (`>= 80%`, targeting `~100%`).
   - Re-run `pwsh -File .agent/skills/security-audit/scripts/security-prescan.ps1 -Scope <Scope>` to verify findings are resolved and no new issues were introduced.
   - Update the ephemeral audit report artifact with each finding's remediation status and verification evidence.
