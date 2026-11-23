# GUTS Security Audit Report

> [!CAUTION]
> **Confidentiality Notice**: This report is an ephemeral audit artifact. Do **not** commit unpatched vulnerability details, exploit payloads, or sensitive configuration excerpts to the Git repository.

## 1. Audit Metadata

- **Date**: YYYY-MM-DD
- **Scope**: `<All | Backoffice | BackofficeDotNet | BackofficeAngular | DotNetClient | JavaClients | DevOps | Diff>`
- **Standards Applied**: OWASP Top 10, OWASP API Security Top 10, OWASP ASVS, CWE, GUTS Threat Model
- **Automated Checks Executed**:
  - [ ] `security-prescan.ps1 -Scope <Scope>`
  - [ ] `dotnet list package --vulnerable --include-transitive` (Backoffice / DotNetClient)
  - [ ] `npm audit` (Backoffice Angular SPA)
  - [ ] Gradle dependency & wrapper inspection (JavaClients)
  - [ ] `Guts.Architecture.Tests`
  - [ ] SonarCloud / `SonarAnalyzer.CSharp`
  - [ ] Optional external scanners (`trivy` / `gitleaks` / `semgrep` — list if run, or mark `Not installed`)
- **Skipped Checks & Reason**: `<None | e.g., Offline sandbox skipped online NuGet/npm vulnerability DB lookup>`

---

## 2. Executive Summary

| Severity | Count | Key Themes |
| :--- | :---: | :--- |
| **Critical** | 0 | |
| **High** | 0 | |
| **Medium** | 0 | |
| **Low** | 0 | |
| **Total** | **0** | |

---

## 3. Prioritized Findings Summary

| ID | Severity | Area | Title | OWASP / API | CWE | Location | Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `SEC-001` | Critical / High / Medium / Low | Backoffice API / Angular / DotNetClient / JavaClients / DevOps | Short title | e.g. A01 / API1 | CWE-639 | `path/to/File.cs#L10-L25` | Pending Approval |

---

## 4. Detailed Findings

### SEC-001 — `<Short Descriptive Title>`

- **Severity**: `<Critical | High | Medium | Low>`
- **Area**: `<Backoffice .NET API | Backoffice MCP | Backoffice Angular | DotNetClient | JavaClients | DevOps/CI>`
- **Classification**: `<OWASP Top 10> | <OWASP API Top 10 (if applicable)> | <CWE-XXX> | <ASVS X.Y.Z>`
- **Location**: [File.cs](file:///c:/Workspace/PXL-IT-NET/Guts/path/to/File.cs#L10-L25)
- **Evidence & Root Cause**:
  Explain concretely what the code/config currently does and why existing mitigations (global filters, framework defaults) do not stop it.
- **Attack Scenario / Impact**:
  Describe who can exploit this (Unauthenticated / Student / Lector / Local user / CI attacker) and the concrete impact on GUTS confidentiality, integrity (e.g., exam/test scores), or availability.
- **Recommended Fix**:
  Concrete, idiomatic remediation aligned with GUTS architecture (specify layer: `Guts.Domain`, `Guts.Application`, `Guts.Api`, Angular, Client, or CI/Docker). Note if breaking for clients/users.
- **TDD Security Regression Test**:
  Specify the failing abuse-case test to write first (project + test scenario).
- **Systemic Guardrail (Optional)**:
  Specify any `Guts.Architecture.Tests`, ESLint, analyzer, or CI rule to prevent recurrence.

---

## 5. Audited Suppressions & Accepted Risks

Review existing `[SuppressMessage("Security", ...)]`, `#pragma warning disable S*`, and `eslint-disable` directives in scope:

| Location | Rule / Directive | Stated Justification | Audit Verdict |
| :--- | :--- | :--- | :--- |
| `path/File.cs#L1` | `S6781` | ... | Valid / Needs Tightening / Remove |

---

## 6. Stage 2 Approval Prompt

Reply with which findings you want to remediate in Stage 2 (e.g., *"Fix all Critical and High findings"*, *"Fix SEC-001 and SEC-003"*, or *"Create an SDD spec for SEC-002 first"*).
