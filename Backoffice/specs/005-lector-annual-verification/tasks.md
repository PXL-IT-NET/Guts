# TDD Implementation Plan: Annual Lector Email Re-Verification & Account Unlock

- **Feature ID**: `005-lector-annual-verification`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-1, DR-1, DR-2]` Update `UserBuilder` in `Guts.Domain.Tests/Builders/` to support `WithLastVerifiedOnUtc(DateTime? utcTimestamp)`.
- [x] **[T1.2]** `[DR-1, DR-2]` Write failing unit tests in `Guts.Domain.Tests/UserAggregate/UserTests.cs` for `MarkEmailVerified` (UTC normalization, invalid default timestamp `ContractException`) and `RequiresAnnualVerification` (null, exactly 1 year ago, 364 days ago, leap-year boundary, < 1 year).
- [x] **[T1.3]** `[AC-2, DR-3, DR-4]` Write failing unit tests in `Guts.Domain.Tests/UserAggregate/LectorVerificationPolicyTests.cs` covering `IsLocked` (student, non-lector admin, unverified lector, expired lector, dual lector+admin, recently verified lector) and `EnsureCanUnlock` invariants.
- [x] **[T1.4]** `[AC-1, AC-2, DR-1..4]` Implement `LastVerifiedOnUtc`, `MarkEmailVerified`, and `RequiresAnnualVerification` on `User.cs` and create `LectorVerificationPolicy.cs` in `Guts.Domain/UserAggregate/`.
- [x] **[T1.5]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Application Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-3]` Extend `IMailSender` with `SendLectorVerificationEmailMessageAsync` and update `MailSender.cs` (with unit tests in `Guts.Application.Tests/Communication/MailSenderTests.cs`).
- [x] **[T2.2]** `[AC-7]` Update `IUserRepository` (`SearchUsersAsync` `lockedOnly` overload, `UpdateAsync`) and `AdminUserDto` (`LastVerifiedOnUtc`, `IsLockedLector`, `CanUnlockLector`) and `IAdminUserService` (`SearchUsersAsync`, `UnlockLectorAsync`).
- [x] **[T2.3]** `[AC-7]` Write failing unit tests in `Guts.Application.Tests/Services/AdminUserServiceTests.cs` for `SearchUsersAsync` (populating lock flags + forwarding `lockedOnly`) and `UnlockLectorAsync` (valid unlock, `DataNotFoundException`, `ContractException` for non-lector / already verified).
- [x] **[T2.4]** `[AC-7]` Implement `AdminUserService.SearchUsersAsync` and `AdminUserService.UnlockLectorAsync`.
- [x] **[T2.5]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-1]` Add EF Core migration `AddUserLastVerifiedOnUtc` in `Guts.Infrastructure/Migrations/` (nullable `LastVerifiedOnUtc` on `AspNetUsers`).
- [x] **[T3.2]** `[AC-7]` Write failing repository integration tests in `Guts.Infrastructure.Tests/Repositories/UserDbRepositoryTests.cs` covering `LastVerifiedOnUtc` persistence via `UpdateAsync` and `SearchUsersAsync(..., lockedOnly: true)` against relational SQLite.
- [x] **[T3.3]** `[AC-7]` Implement `FilterLockedLectors` and `UpdateAsync` in `Guts.Infrastructure/Repositories/UserDbRepository.cs`.
- [x] **[T3.4]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-2, AC-3, AC-4, AC-5, AC-6]` Write failing unit tests in `Guts.Api.Tests/Controllers/AuthControllerTests.cs` covering:
  - `CreateToken`: locked lector with valid password gets `403 Forbidden` (`LectorVerificationRequired` with `userId`, `email`, `resendToken`), sends verification email, sets no `LoginSession` token; locked lector with wrong password gets `401 Unauthorized` and sends no mail; verified lector succeeds (`200 OK`); student/admin-only never locked.
  - `ResendLectorVerification`: valid `resendToken` sends a fresh verification email and returns `200 OK`; invalid token returns `400 Bad Request`; unknown user returns `200 OK`.
  - `VerifyLector`: valid token sets `EmailConfirmed = true`, updates `LastVerifiedOnUtc`, saves via `UpdateAsync`, returns `200 OK`; invalid token returns `400 Bad Request`; unknown user returns `200 OK`.
  - `ConfirmEmail` & `ResetPassword`: update `LastVerifiedOnUtc` on success.
- [x] **[T4.2]** `[AC-7]` Write failing unit tests in `Guts.Api.Tests/Controllers/AdminUserControllerTests.cs` for `SearchUsers` (`lockedOnly`) and `UnlockLector` (`204 NoContent`, `400 BadRequest`, `404 NotFound`).
- [x] **[T4.3]** `[AC-2..7]` Implement `VerifyLectorModel`, `ResendLectorVerificationModel`, `LectorVerificationRequiredModel`, and updates to `AuthController.cs` and `AdminUserController.cs`.
- [x] **[T4.4]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T5.1]** `[AC-3, AC-4, AC-5, AC-7]` Update/add Jasmine unit tests (`auth.service.spec.ts`, `login.component.spec.ts`, `lector-verification-sent.component.spec.ts`, `verify-lector.component.spec.ts`, `admin-user.service.spec.ts` / `admin-users.component.spec.ts`).
- [x] **[T5.2]** `[AC-3..5]` Implement `LoginResult`, `VerifyLectorModel`, `ResendLectorVerificationModel`, `AuthService.login` / `AuthService.verifyLector` / `AuthService.resendLectorVerification`, `LoginComponent` redirect, `LectorVerificationSentComponent` (with Resend button), `VerifyLectorComponent`, and routing/module registrations.
- [x] **[T5.3]** `[AC-7]` Update `AdminUser`, `AdminUserService`, and `AdminUsersComponent` (`Locked only` toggle, Verification badge column, `Unlock` button + confirmation modal).
- [x] **[T5.4]** Add/update Playwright E2E test coverage in `Backoffice/Guts.Web/ClientApp/e2e/tests/admin-and-impersonation.spec.ts` / `login.spec.ts` covering locked lector login -> mail sent page -> verify link -> login unlocked, and admin overview unlock modal.
- [x] **[T5.5]** Run `npm run lint`, `npm test -- --watch=false`, `npm run build`, and Playwright E2E tests in `Backoffice/Guts.Web/ClientApp`.

---

## Phase 6: Final Verification & Quality Gate

- [x] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`.
- [x] **[T6.2]** Run frontend checks: `npm run lint`, `npm test -- --watch=false`, `npm run build`, and relevant Playwright E2E suites.
- [x] **[T6.3]** Run SonarCloud analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`), check CE/Quality Gate status (`CE=SUCCESS`, `QG=OK`, `OPEN_ISSUES=0`, `OPEN_HOTSPOTS=0`), and resolve any reported issues.
- [x] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
