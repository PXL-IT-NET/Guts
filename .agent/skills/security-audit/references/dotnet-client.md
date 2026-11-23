# Security Audit Checklist — .NET Client Libraries (`DotNetClient/`)

Use this checklist when auditing `DotNetClient/` (`Guts.Client.Core`, `Guts.Client.NUnit`, `Guts.Client.XUnit`, `Guts.Client.WPF`, and their test/build configuration).

---

## 1. Local Token Caching & Credential Protection (OWASP A02, A07 / CWE-312, CWE-522, CWE-732)

Inspect `DotNetClient/Guts.Client.Core/Utility/AuthorizationHandler.cs`, `LoginWindow.cs`, `LoginSession.cs`:

- **Plaintext Token Storage (`%APPDATA%/Guts/_cache.txt`)**:
  - `AuthorizationHandler.StoreTokenLocally` writes the raw JWT bearer token to `_cache.txt`.
  - Audit whether the token should be encrypted at rest (e.g., Windows DPAPI `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)` with safe cross-platform fallback and restrictive file permissions `0600` on Linux/macOS / owner-only ACL on Windows).
  - Check for race conditions or partial writes when multiple test runners execute concurrently (atomic write / file locking).
- **Login Flow & Callback Security (`LoginWindow.cs`, `LoginSession.cs`)**:
  - Verify the login URL uses HTTPS (no plaintext HTTP fallback outside explicit localhost dev testing).
  - If an embedded browser / WebView / local HTTP listener or polling session (`LoginSession`) is used:
    - Verify session tokens are high-entropy CSPRNG values, bound to short TTLs, and cannot be hijacked by another local user or cross-origin page (CSRF / state parameter).
    - Verify credentials or tokens are never written to test runner stdout/stderr (`ITestOutputWriter`), debug logs, or exception messages (CWE-532).

---

## 2. Transport Security & HTTP Client Hardening (OWASP A02, A05, A10 / CWE-295, CWE-319)

Inspect `HttpClientToHttpHandlerAdapter.cs`, `TestRunResultSender.cs`, `LoginSession.cs`:

- **HTTPS & Certificate Validation**:
  - Ensure API base URLs enforce HTTPS (`https://`) in production and never disable certificate validation (`ServerCertificateCustomValidationCallback = ... => true`, `DangerousAcceptAnyServerCertificateValidator`, `ServicePointManager.ServerCertificateValidationCallback`).
- **Bearer Token Leakage on Redirects**:
  - Verify `HttpClient` does not forward `Authorization: Bearer ...` headers across host/scheme redirects (standard `HttpClient` strips on cross-host redirects, but custom handlers or manual retries can re-attach it!).
- **Timeouts, Retries & Response Handling**:
  - Ensure reasonable HTTP timeouts are set so stalled/malicious servers cannot hang IDE test runs indefinitely.
  - Check JSON deserialization (`System.Text.Json` / `Newtonsoft.Json`) for unsafe `TypeNameHandling` (CWE-502 — `TypeNameHandling.All/Auto/Objects/Arrays` enables RCE!) and unbounded response payload sizes.

---

## 3. Solution File Extraction, Path Traversal & Secret Exfiltration (OWASP A01, A03 / CWE-22, CWE-200)

Inspect `SourceCodeRetriever.cs`, `Solution.cs`, `FileUtil.cs`:

- **Path Traversal & Symlink Escape (CWE-22, CWE-59)**:
  - Verify configured `SourceCodeRelativeFilePaths` (e.g., passed via `[ExerciseTestFixture]` or `[MonitoredTest]`) are canonicalized with `Path.GetFullPath(...)` and strictly verified to stay within the solution root directory (reject `..` escaping outside the repo/solution directory, absolute paths like `C:\Users\...\id_rsa`, UNC paths `\\attacker\share`, and symlinks pointing outside root).
- **Accidental Secret Exfiltration (CWE-200)**:
  - Verify file extraction/globbing only uploads intended source code files (e.g., `.cs`, `.xaml`, `.xml`, `.json` allowlist or explicit exclusion of `.env`, `appsettings.Development.json`, `secrets.json`, private keys `*.pem`/`*.pfx`, `.git/`).
  - Check file size limits before reading files into memory and uploading to Backoffice.

---

## 4. Anti-Tampering Code Cleaning & Hashing (OWASP A02, A08 / CWE-327, CWE-1333)

Inspect `CodeCleaner.cs`, `TestRunResultAccumulator.cs`, and NUnit/XUnit fixture attributes:

- **Cryptographic Hash Strength (CWE-327)**:
  - Inspect which hash algorithm computes the test code hash (flag MD5 or SHA-1 — prefer SHA-256; note hash changes require coordinated Backoffice + DotNetClient + JavaClients rollout!).
- **ReDoS in `CodeCleaner.cs` (CWE-1333)**:
  - Audit all regular expressions stripping comments/whitespace/attributes for catastrophic backtracking on crafted input; enforce `Regex` timeouts (`TimeSpan.FromSeconds(...)`) or `RegexOptions.NonBacktracking` where target framework allows.
- **Tampering Surface**:
  - Verify `CodeCleaner` cannot be tricked by malformed comments/strings/preprocessor directives (`#if`, `#region`) into producing identical hashes for materially altered test assertions.

---

## 5. Supply Chain, Build & NuGet Security (OWASP A06, A08)

- Run `dotnet list DotNetClient/Guts.sln package --vulnerable --include-transitive`.
- Inspect `DotNetClient/Guts.Client.Core/buildTransitive/Guts.Client.Core.targets` and `.csproj` files for unsafe MSBuild `<Exec>` tasks, untrusted NuGet feeds, or deprecated target frameworks / packages.
- Check `Guts.Client.WPF` helpers (`FindVisualChildren<T>()`, reflection utilities in `ObjectExtensions.cs`) for unintended unsafe reflection or arbitrary code invocation when inspecting student assemblies.
