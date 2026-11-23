# Security Audit Checklist — Java Client Libraries (`JavaClients/`)

Use this checklist when auditing `JavaClients/` (`guts-client-core`, `guts-client-junit-5`, `guts-client-junit-5-example`, Gradle build & wrapper configuration).

---

## 1. Local Token Caching & Swing Credential Handling (OWASP A02, A07 / CWE-312, CWE-522, CWE-732)

Inspect `be.pxl.guts.core.auth` (`AbstractAuthorizationHandler.java`, `SwingAuthorizationHandler.java`, `SwingLoginWindow.java`) and `Credentials.java`:

- **Token File Storage & Permissions (`LOCAL_TOKEN_FILE`)**:
  - `AbstractAuthorizationHandler.storeTokenLocally` writes the raw bearer token via `Files.writeString(LOCAL_TOKEN_FILE, token)` with default OS umask permissions.
  - Verify token directory and file restrict permissions to owner-only (`PosixFilePermissions.fromString("rw-------")` on POSIX / owner-only ACL via `AclFileAttributeView` on Windows) and evaluate encrypting cached tokens at rest.
  - Ensure `LOCAL_TOKEN_FILE` path cannot be hijacked via symlink/shared temp directory (`CWE-377`).
- **Password & Credential Memory Hygiene (`SwingLoginWindow.java`, `Credentials.java`)**:
  - Verify Swing password inputs use `JPasswordField.getPassword()` (`char[]`) rather than `getText()` (`String`), and clear `char[]` buffers (`Arrays.fill(pwd, '\0')`) after authentication.
  - Ensure credentials and bearer tokens are **never** logged via `GutsLogger` / `java.util.logging.Logger` (including exception messages or HTTP payload dumps — CWE-532).

---

## 2. HTTP Client, TLS & Serialization Security (OWASP A02, A06, A08 / CWE-295, CWE-319, CWE-502)

Inspect `be.pxl.guts.core.http` (`GutsHttpClient.java`, `TestResultSender.java`, `ApiRoutes.java`) and `be.pxl.guts.core.config` (`BasicConfig.java`, `Configuration.java`):

- **Legacy Apache HttpClient & TLS Configuration**:
  - `GutsHttpClient.java` uses `org.apache.http.impl.client.HttpClientBuilder` (Apache HttpComponents 4.x — check exact version in `build.gradle` for known CVEs and EOL status; consider migrating to `java.net.http.HttpClient` or Apache HttpClient 5.x).
  - Verify `GutsHttpClient` sets explicit `RequestConfig` timeouts (`connectTimeout`, `socketTimeout`, `connectionRequestTimeout`) and never disables TLS certificate verification or hostname verification (`NoopHostnameVerifier`, `TrustAllStrategy`).
  - Verify `config.getApiUrl()` validates scheme (`https://` in production) and rejects user-controlled URL redirection/SSRF via untrusted `guts.json` configs.
- **Bearer Token & Response Handling**:
  - Verify `Authorization: Bearer ...` is not sent over plaintext HTTP or leaked on cross-host redirects.
  - Ensure `HttpResponse` entities are consumed/closed properly (`EntityUtils.consumeQuietly` / try-with-resources) with bounded read limits to prevent resource leaks or OOM.
  - Check `Gson` usage and ensure `ObjectInputStream` / Java native deserialization is never used on untrusted data (CWE-502).
  - Note `GutsHttpClient.post(...)` uses `new StringEntity(data)` without explicit `StandardCharsets.UTF_8` — check encoding consistency and error swallowing (`return null` hiding auth/TLS errors).

---

## 3. Solution File Extraction, Path Traversal & Hashing (OWASP A01, A02, A03 / CWE-22, CWE-327, CWE-1333)

Inspect `be.pxl.guts.core.util` (`FileUtil.java`, `CodeCleaner.java`, `TestAccumulator.java`) and `guts-client-junit-5`:

- **Path Traversal & Sensitive File Exclusion (CWE-22, CWE-200)**:
  - Verify `FileUtil` resolves paths via `.toPath().toAbsolutePath().normalize()` and enforces `.startsWith(projectRoot)` so configured source file patterns in `@GutsFixture` or `guts.json` cannot escape the project directory or exfiltrate `~/.ssh`, `.env`, or system files.
  - Verify symlink traversal and file size caps are enforced before reading/uploading `SolutionFile` payloads.
- **Code Cleaning & Anti-Tampering Hash (`CodeCleaner.java`, `TestAccumulator.java`)**:
  - Inspect cryptographic hash algorithm (`MessageDigest.getInstance(...)` — flag `MD5`/`SHA-1`, prefer `SHA-256` coordinated with Backoffice).
  - Audit all `Pattern.compile(...)` regexes in `CodeCleaner.java` for catastrophic backtracking (ReDoS / CWE-1333) and verify parity with `DotNetClient`'s cleaning semantics so students cannot bypass hash verification via comment/string tricks.

---

## 4. Gradle Supply Chain & Build Integrity (OWASP A06, A08)

Inspect `JavaClients/settings.gradle`, `JavaClients/*/build.gradle`, and `JavaClients/gradle/wrapper/gradle-wrapper.properties`:

- **Dependency Vulnerabilities**:
  - Check every dependency version in `guts-client-core/build.gradle`, `guts-client-junit-5/build.gradle`, and `guts-client-junit-5-example/build.gradle` (e.g., `httpclient`, `gson`, `junit`, `mockito`) against known CVEs (use `trivy` / `osv-scanner` if available, or Gradle dependency verification).
  - Verify all Maven repositories use `mavenCentral()` over HTTPS (never `http://` or deprecated `jcenter()`).
- **Gradle Wrapper Integrity**:
  - Inspect `gradle-wrapper.properties` for outdated Gradle versions, HTTPS `distributionUrl`, and presence of `distributionSha256Sum` to prevent wrapper tampering (CWE-494).
- **Example Project Hygiene (`guts-client-junit-5-example/guts.json`)**:
  - Verify no real credentials, tokens, or internal staging secrets are committed in `guts.json` or test resources.
