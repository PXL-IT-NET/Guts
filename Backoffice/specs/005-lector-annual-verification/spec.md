# Specification: Annual Lector Email Re-Verification & Account Unlock

- **Feature ID**: `005-lector-annual-verification`
- **Status**: `Implemented`
- **Created**: `2026-11-15`

---

## 1. Context & Problem Statement

Some users with the `lector` role in GUTS are no longer active lectors at PXL. Because an active lector always retains access to their `@pxl.be` mailbox, annual re-verification of `@pxl.be` email ownership prevents former lectors from retaining long-term lecturer access in GUTS.

This feature locks any `lector` account that has not been verified for one year or longer (including existing lectors who have never been marked as verified), blocks login until verification completes, sends a verification link to their email address, unlocks the account when the link is clicked, and lets Administrators inspect and manually unlock locked lectors from the Admin Users overview.

---

## 2. User Stories & Actors

- **Actors**: `Lector (Teacher) | Administrator | IDE Test Client (via Browser Login Session)`

- **US-1**: As an **Administrator**, I want `lector` accounts to be locked automatically after one year without email verification so that former staff lose access unless they still control their `@pxl.be` mailbox.
- **US-2**: As a **locked Lector**, when I attempt to log in with valid credentials, I want to see a clear page informing me that a verification email was sent to my email address and that my access is blocked until I click the link in that email.
- **US-3**: As a **locked Lector**, when I click the verification link sent to my `@pxl.be` inbox, I want my account to be unlocked for another year and be directed to log in normally.
- **US-4**: As an **Administrator**, I want to filter and see which lectors are currently locked in the Admin Users overview and manually unlock a lector (with a confirmation modal) when needed.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Track Verification Timestamp & Rollout Initialization
- **Given** a `User` entity in the system
- **When** the database schema is migrated for this feature
- **Then** `User` stores a nullable UTC timestamp `LastVerifiedOnUtc` (`DateTime?`), initialized to `null` for all existing users so every existing `lector` is required to re-verify on their next login.

### AC-2: Annual Verification Rule Scope
- **Given** a user authenticating via `POST /api/auth/token`
- **When** the user holds the `lector` role (including users who hold both `lector` and `admin` roles) and `RequiresAnnualVerification` is `true` (`LastVerifiedOnUtc == null` or `LastVerifiedOnUtc <= utcNow.AddYears(-1)`)
- **Then** the user is considered locked for annual re-verification.
- **And** users who do **not** hold the `lector` role (`student`-only or non-lector `admin`-only) are never locked by this rule.

### AC-3: Locked Lector Login Blocked & Email Sent
- **Given** a locked `lector` submits valid email and password on `/login` (including IDE browser sessions `/login?s=...`) or `POST /api/auth/token`
- **When** credentials pass password validation and initial email confirmation (`EmailConfirmed == true`)
- **Then** the backend:
  - Does **not** issue a JWT access token and does **not** attach a token to any `LoginSession`
  - Generates a user verification token (`LectorVerification` purpose) and sends a dedicated lector verification email containing `/verify-lector?userId=<id>&token=<urlEncodedToken>` via `IMailSender`
  - Generates a signed resend proof token (`LectorVerificationResend` purpose) for the authenticated caller
  - Returns `403 Forbidden` with structured payload `{ code: "LectorVerificationRequired", userId: <user.Id>, email: "<user.Email>", resendToken: "<token>", message: "..." }`
- **And** submitting an invalid password for a locked lector still returns `401 Unauthorized` without sending a verification email or revealing lock status.

### AC-4: Dedicated "Verification Mail Sent" Page & Resend Action
- **Given** a locked `lector` attempts to log in from the Angular `/login` page (standard or `/login?s=...` IDE flow)
- **When** `POST /api/auth/token` responds with `403 Forbidden` (`code === "LectorVerificationRequired"`)
- **Then** the user is navigated to `/lector-verification-sent` displaying:
  - Confirmation that a verification email has been sent to their email address (`state.email` if available)
  - Clear notice that account access remains blocked until they click the link in the email
  - A **"Resend verification email"** button (when `userId` and `resendToken` state are present) that calls `POST /api/auth/resend-lector-verification` and displays a confirmation alert when a fresh email is sent
  - A button/link back to `/login`

### AC-5: Email Verification Link Unlocks Lector Account
- **Given** a locked `lector` opens the emailed link `/verify-lector?userId=<id>&token=<token>`
- **When** `VerifyLectorComponent` submits `{ userId, token }` to `POST /api/auth/verifylector`
- **Then** the backend validates the token via ASP.NET Identity (`VerifyUserTokenAsync`), sets `EmailConfirmed = true` if needed, updates `LastVerifiedOnUtc = utcNow`, persists the user, and returns `200 OK`.
- **And** the UI displays confirmation that the lector account is unlocked and renders a **"Log in"** button routing to `/login` (no automatic JWT sign-in occurs).
- **And** invalid/expired tokens return `400 Bad Request` and display an error alert asking the user to log in again to receive a new verification link.

### AC-6: Registration Confirmation & Password Reset Refresh Verification
- **Given** a user completes `POST /api/auth/confirmemail` (initial registration) or `POST /api/auth/resetpassword` (email-based password reset)
- **When** ASP.NET Identity succeeds
- **Then** `LastVerifiedOnUtc` is also updated to `utcNow` and saved on the user.

### AC-7: Admin Users Overview — Visibility, Filter & Manual Unlock with Confirmation Modal
- **Given** an authenticated `admin` on `/admin/users` (`AdminUserController` / `AdminUsersComponent`)
- **When** viewing and filtering users
- **Then**:
  - `AdminUserDto` includes `lastVerifiedOnUtc`, `isLockedLector`, and `canUnlockLector`
  - `GET /api/admin/users` supports `[FromQuery] bool lockedOnly = false` to filter down to currently locked lectors
  - The Admin Users table renders a **Verification** column showing a `Locked` badge or `Verified` badge (with `LastVerifiedOnUtc` tooltip) for `lector` users, and `—` for non-lectors
  - The search bar includes a **"Locked only"** toggle switch alongside **"Orphaned only"**
  - Locked lector rows expose an **"Unlock"** button that opens a confirmation modal before calling `POST /api/admin/users/{userId}/unlock`
  - Confirming unlocks the lector (`LastVerifiedOnUtc = utcNow`), closes the modal, shows a success toast, and refreshes the table.

---

## 4. Domain Rules & Invariants

- **DR-1**: `User.MarkEmailVerified(DateTime verifiedOnUtc)` requires `verifiedOnUtc != default` (`Contracts.Require`) and stores UTC `LastVerifiedOnUtc`.
- **DR-2**: `User.RequiresAnnualVerification(DateTime utcNow)` returns `true` iff `LastVerifiedOnUtc == null || LastVerifiedOnUtc.Value <= utcNow.AddYears(-1)`.
- **DR-3**: `LectorVerificationPolicy.IsLocked(User user, IReadOnlyCollection<string> roles, DateTime utcNow)` returns `true` iff `user != null`, `roles` contains `Role.Constants.Lector`, and `user.RequiresAnnualVerification(utcNow)`.
- **DR-4**: `LectorVerificationPolicy.EnsureCanUnlock(User targetUser, IReadOnlyCollection<string> targetRoles, DateTime utcNow)` enforces via `Contracts.Require` that `targetUser != null`, `targetRoles` contains `Role.Constants.Lector`, and the lector is currently locked.

---

## 5. Authorization & Security Rules

- **Password Gate Before Lock Disclosure**: `POST /api/auth/token` must validate the password **before** checking `EmailConfirmed` or annual lector verification so unauthenticated attackers cannot trigger emails or probe lock state.
- **No Auto-Login from Email Link**: `POST /api/auth/verifylector` unlocks the account without returning a JWT, preventing email link scanners or forwarded links from granting access tokens.
- **No IDE Login Session Pollution**: When `CreateToken` rejects a locked lector, `loginSessionService.SetLoginTokenForSessionAsync` is not called.
- **Active JWT Lifetime**: Existing valid JWTs continue until natural expiration (`ExpirationTimeInMinutes: 2160` / 36 hours) without adding per-request DB queries.
- **Admin Endpoints**: `GET /api/admin/users` and `POST /api/admin/users/{userId}/unlock` require `ApiConstants.AdminOnlyPolicy`.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Locked lector submits wrong password | Logs warning; no email sent; no lock disclosure | `401 Unauthorized` |
| Locked lector submits valid password | Sends lector verification email; blocks login | `403 Forbidden` (`LectorVerificationRequired`) |
| Invalid or expired token on `POST /api/auth/verifylector` | ModelState errors returned from Identity | `400 Bad Request` |
| Non-existent user ID on `POST /api/auth/verifylector` | Returns `200 OK` without revealing account absence (matches `ConfirmEmail`) | `200 OK` |
| Admin unlocks non-existent user | Throws `DataNotFoundException` | `404 Not Found` |
| Admin unlocks student or already-verified lector | Throws `ContractException` (`Contracts.Require`) | `400 Bad Request` |
| Invalid `userId` (`< 1`) on admin unlock | Rejected by controller validation | `400 Bad Request` |
| Direct navigation to `/lector-verification-sent` without router state | Displays generic confirmation message without crashing | `200 OK` (SPA view) |

---

## 7. Out of Scope

- Per-request middleware revoking already-issued 36-hour JWTs immediately upon reaching the 1-year boundary.
- Recurring background jobs or scheduled reminder emails before a lector's verification expires.
- Manual admin "Lock" button or batch unlock action in `/admin/users`.

---

## 8. Open Questions

_None — all design decisions were resolved during requirements review._
