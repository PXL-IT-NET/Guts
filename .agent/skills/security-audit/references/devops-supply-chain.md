# Security Audit Checklist — DevOps, Containers, CI/CD & Repository Hygiene

Use this checklist when auditing Dockerfiles, `docker-compose*.yml`, `.github/workflows/`, `Backoffice/deploy/`, and repository-wide secret/configuration hygiene.

---

## 1. Secrets Management & Repository Hygiene (OWASP A02, A05 / CWE-798, CWE-200, CWE-532)

- **Hardcoded Secrets & Credentials**:
  - Audit `Backoffice/Guts.Api/appsettings*.json`, `Backoffice/Guts.Web/appsettings*.json`, `Backoffice/docker-compose*.yml`, `Backoffice/deploy/*`, and `JavaClients/**/guts.json` for real JWT signing keys, database passwords, SMTP credentials, reCAPTCHA secret keys, or API tokens.
  - Ensure production/staging secrets are injected strictly via environment variables, IIS machine/user secrets, or GitHub Environment Secrets — never committed files.
- **Committed Sensitive Artifacts & Logs**:
  - Check for committed log files (e.g., `Backoffice/Guts.Api/Log/*.log` — logs can contain PII, student emails, IP addresses, stack traces, or tokens and should be gitignored!).
  - Check `.gitignore` covers `.sonar-token`, `.sonarcloud-token`, `appsettings.Local.json`, `*.pfx`, `*.pem`, `*.key`, `_cache.txt`, and `Log/`.
- **SonarQube / Analyzer Suppressions**:
  - Audit every `[SuppressMessage("Security", ...)]` and `#pragma warning disable S*` across the repo to ensure none hide real production vulnerabilities.

---

## 2. Container & Docker Security (OWASP A05 / CWE-250, CWE-1188)

Inspect `Backoffice/Guts.Api/Dockerfile`, `Backoffice/Guts.Web/ClientApp/Dockerfile`, `Backoffice/.dockerignore`, and `Backoffice/docker-compose*.yml`:

- **Least Privilege Runtime User (CWE-250)**:
  - Verify final container stages run as a non-root user (`USER $APP_UID` or dedicated non-root user; `Guts.Api/Dockerfile` uses `USER $APP_UID` — verify `Guts.Web/ClientApp/Dockerfile` does too!).
- **Base Image Hygiene & Pinning**:
  - Avoid `:latest` tags; prefer pinned major.minor (or SHA256 digest) official images (`mcr.microsoft.com/dotnet/aspnet:10.0`, `node:22-alpine`, `nginx:*-alpine`).
  - Ensure build SDKs (`dotnet/sdk`, `node` build tools) are stripped in multi-stage builds so they do not exist in the final runtime image.
- **`.dockerignore` Completeness**:
  - Verify `.dockerignore` excludes `.git`, `Log/`, `*.user`, `appsettings.Development.json`, `.sonar*-token`, `node_modules`, `TestResults`, and test projects so secrets and bloat are not baked into image layers.
- **Compose & Network Exposure**:
  - Audit `docker-compose.yml`, `docker-compose.override.yml`, and `docker-compose.sonarqube.yml`:
    - Bind dev/admin ports (DB, SonarQube) to `127.0.0.1` instead of `0.0.0.0` where possible.
    - Flag default weak passwords (e.g., `admin`/`admin` in `run-sonarqube.ps1` / compose) and ensure dev overrides cannot accidentally be used in production.

---

## 3. GitHub Actions CI/CD Security (OWASP A08 / CWE-829, CWE-94)

Inspect `.github/workflows/backoffice-production.yml`, `.github/workflows/backoffice-staging.yml`, and any other workflow files:

- **Least-Privilege `GITHUB_TOKEN` Permissions**:
  - Verify top-level `permissions: contents: read` (explicit least privilege) is declared instead of relying on repository default write permissions.
- **Action Pinning (Supply Chain / CWE-829)**:
  - Flag mutable tag references (`uses: actions/checkout@v4`) vs full commit SHA pinning (`uses: actions/checkout@<40-char-sha> # v4`) for high-security production release workflows.
- **Script Injection (CWE-94)**:
  - Check every `run:` block for unsafe `${{ github.event.* }}`, `${{ github.head_ref }}`, or untrusted inputs interpolated directly into PowerShell/Bash strings (must pass via `env:` instead).
- **Self-Hosted Runner Hardening (`runs-on: [self-hosted, Windows]`)**:
  - Verify workflows running on self-hosted Windows IIS runners only trigger on protected branches (`main`/`master`, never untrusted `pull_request` or `pull_request_target` from forks!), require GitHub Environment manual approvals (`environment: production`), and clean up sensitive workspace artifacts.
  - Audit `Backoffice/deploy/deploy-to-iis.ps1` for parameter validation, safe path handling, least-privilege IIS app pool permissions, and config/secret preservation.
- **CI Security Gates**:
  - Check whether CI runs `npm run lint`, `dotnet list package --vulnerable`, `npm audit`, and SonarCloud quality gates before producing production deploy artifacts.
