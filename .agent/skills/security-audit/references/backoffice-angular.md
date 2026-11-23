# Security Audit Checklist — Backoffice Angular SPA & `Guts.Web` Host

Use this checklist when auditing `Backoffice/Guts.Web/` (ASP.NET Core SPA host) and `Backoffice/Guts.Web/ClientApp/` (Angular 22 SPA).

---

## 1. Cross-Site Scripting (XSS) & DOM Sanitization (OWASP A03 / CWE-79)

Angular escapes template interpolations (`{{ ... }}`) by default, so focus on bypasses and dynamic sinks:

- **Angular Sanitizer Bypasses**:
  - Search for `DomSanitizer` and every call to `bypassSecurityTrustHtml`, `bypassSecurityTrustScript`, `bypassSecurityTrustStyle`, `bypassSecurityTrustUrl`, `bypassSecurityTrustResourceUrl`.
  - Verify no student-controlled data (names, team names, solution code, test failure messages, markdown/comments) flows into any `bypassSecurityTrust*` call.
- **Dangerous HTML/DOM Sinks**:
  - Audit `[innerHTML]`, `[outerHTML]`, `[srcdoc]`, `[href]`, `[src]`, `ElementRef.nativeElement.innerHTML`, `Renderer2`, `document.write`, `eval`, `new Function`, and `jQuery`/Bootstrap tooltip/popover `html: true` options (Bootstrap tooltips/popovers with `html: true` bypass Angular's sanitizer!).
- **Chart.js / Third-Party Rendering**:
  - Check `ng2-charts` / `chart.js` custom HTML tooltip callbacks and `ngx-toastr` `enableHtml: true` usages to ensure user-supplied text is never rendered as raw HTML.
- **File Downloads & Blob URLs**:
  - Inspect `file-saver` usages and `URL.createObjectURL(...)` to verify downloaded/exported filenames are sanitized, CSV cells mitigate formula injection (`=`, `+`, `-`, `@`), and object URLs are revoked (`URL.revokeObjectURL`).

---

## 2. Authentication Tokens, Storage & Interceptors (OWASP A07, A02 / CWE-522, CWE-922)

Inspect `src/app/interceptors/` (`tokeninterceptor.ts`, `impersonation-readonly.interceptor.ts`, `relative.interceptor.ts`) and auth services:

- **Token Storage & Lifecycle**:
  - Audit where JWTs and impersonation tokens are stored (`localStorage` vs `sessionStorage` vs memory/HttpOnly cookies). Document XSS-to-token-exfiltration risk and confirm tokens are completely cleared on logout, 401 expiry, and impersonation exit.
  - Check whether JWTs are ever placed in URL query strings (which leak via browser history, `Referer` headers, and server logs).
- **Bearer Token Leakage (`tokeninterceptor.ts`)**:
  - Verify `tokeninterceptor.ts` **only** attaches the `Authorization: Bearer ...` header to trusted GUTS API origins/relative API URLs — never to arbitrary third-party URLs!
- **Impersonation Read-Only Enforcement (`impersonation-readonly.interceptor.ts`)**:
  - Verify client-side read-only blocking aligns with `ImpersonationReadOnlyFilter` on the backend and handles all mutating HTTP verbs (`POST`, `PUT`, `PATCH`, `DELETE`), remembering that backend enforcement is the true security boundary.

---

## 3. Client-Side Routing, Guards & Open Redirects (OWASP A01 / CWE-601, CWE-602)

Inspect `src/app/guards/` (`auth.guard.ts`, `admin.guard.ts`) and `app-routing` / login redirect flows:

- **Client Guards Are UX Only (CWE-602)**:
  - For every route protected by `auth.guard.ts`, `admin.guard.ts`, or role checks in templates (`*ngIf="isLector"`), verify the corresponding backend API endpoint enforces the exact same policy server-side.
- **Open Redirects (CWE-601)**:
  - Audit `returnUrl` / `redirectUrl` query parameters used after login, logout, or email verification. Ensure redirects validate that the target is a local relative path (starts with `/` and NOT `//` or `https://evil.example`).
- **External Links (`target="_blank"`)**:
  - Verify all external links using `target="_blank"` include `rel="noopener noreferrer"` (CWE-1022).

---

## 4. `Guts.Web` Host & HTTP Security Headers (OWASP A05 / CWE-16, CWE-693, CWE-1021)

Inspect `Backoffice/Guts.Web/Program.cs`, `appsettings*.json`, `web.config` (if present), and `ClientApp/src/index.html`:

- **Security Headers Missing in `Guts.Web/Program.cs`**:
  - **Content-Security-Policy (CSP)**: Restrict `default-src 'self'`, `script-src`, `style-src`, `connect-src` (GUTS API origin), `img-src`, `object-src 'none'`, `base-uri 'self'`, `frame-ancestors 'none'`. Avoid `'unsafe-eval'`; minimize `'unsafe-inline'`.
  - **Clickjacking**: `X-Frame-Options: DENY` + CSP `frame-ancestors 'none'`.
  - **MIME Sniffing**: `X-Content-Type-Options: nosniff`.
  - **Referrer Leakage**: `Referrer-Policy: strict-origin-when-cross-origin` (critical to avoid leaking password reset or verification URLs).
  - **Permissions-Policy**: Disable unused browser features (`camera=(), microphone=(), geolocation=()`).
  - **HSTS**: Verify `UseHsts()` configuration (`MaxAge`, `IncludeSubDomains`) and HTTPS redirection (`UseHttpsRedirection()` or reverse proxy/IIS enforcement).
- **Client Settings Exposure (`ClientSettings`)**:
  - Verify `Guts.Web` endpoints exposing `ClientSettings` (API URL, reCAPTCHA site key) never serialize private secrets (e.g., reCAPTCHA secret key, signing keys, connection strings).
- **Source Maps & Build Config (`angular.json`)**:
  - Verify production builds (`ng build` / `--configuration production`) disable exposed source maps (`sourceMap: false`) and debug flags.

---

## 5. Frontend Supply Chain & Build Security (OWASP A06, A08)

- Run `npm audit` in `Backoffice/Guts.Web/ClientApp` (distinguish production `dependencies` from `devDependencies`, prioritizing reachable runtime vulnerabilities).
- Check `package-lock.json` integrity, `overrides`, and whether `npm ci` is used in CI (`backoffice-production.yml` / `backoffice-staging.yml`).
- Check external CDN `<script>`/`<link>` tags in `index.html` for Subresource Integrity (`integrity="sha384-..." crossorigin="anonymous"`).
- Run `npm run lint` and check whether ESLint rules catch unsafe patterns (`no-eval`, `no-implied-eval`, `@angular-eslint/template/no-call-expression` / security plugins).
