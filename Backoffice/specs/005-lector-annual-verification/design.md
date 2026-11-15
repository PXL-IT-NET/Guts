# Technical Design: Annual Lector Email Re-Verification & Account Unlock

- **Feature ID**: `005-lector-annual-verification`
- **Specification**: [`spec.md`](./spec.md)
- **Status**: `Implemented`

---

## 1. Architectural Overview

The annual lector re-verification workflow spans all layers of the GUTS Backoffice in strict adherence to DDD layering:

1. **Domain (`Guts.Domain`)**: `User` owns its `LastVerifiedOnUtc` timestamp and `MarkEmailVerified` / `RequiresAnnualVerification` invariants. `LectorVerificationPolicy` encapsulates role scope (`lector`) and unlock preconditions.
2. **Application (`Guts.Application`)**: `IMailSender` / `MailSender` sends dedicated lector verification emails; `IUserRepository` and `AdminUserService` orchestrate searching locked lectors and unlocking them via domain policy + `User` methods.
3. **Infrastructure (`Guts.Infrastructure`)**: Adds nullable `LastVerifiedOnUtc` on `AspNetUsers` via EF Core migration, and extends `UserDbRepository` with SQL-translatable filtering for locked lectors and `UpdateAsync`.
4. **API (`Guts.Api`)**: `AuthController` enforces `LectorVerificationPolicy.IsLocked` during `CreateToken`, exposes `POST /api/auth/verifylector`, and refreshes verification on `ConfirmEmail` and `ResetPassword`. `AdminUserController` adds `lockedOnly` filtering and `POST /api/admin/users/{userId}/unlock`.
5. **Frontend (`Guts.Web/ClientApp`)**: Angular adds `/lector-verification-sent` and `/verify-lector` routes, updates `LoginComponent` / `AuthService` to route locked lectors away from login without storing a token, and updates `AdminUsersComponent` with a **Locked only** filter, verification status badges, and an **Unlock** confirmation modal.

---

## 2. Domain Layer (`Guts.Domain`)

### Aggregates, Entities & Domain Policies
- **Modified Type — `User`** (`Guts.Domain/UserAggregate/User.cs`):
  - Property: `public DateTime? LastVerifiedOnUtc { get; private set; }`
  - Method: `public void MarkEmailVerified(DateTime verifiedOnUtc)`
    - Enforces `Contracts.Require(verifiedOnUtc != default, "Verification timestamp cannot be default.");`
    - Normalizes `verifiedOnUtc` to UTC (`DateTimeKind.Utc`) and assigns `LastVerifiedOnUtc`.
  - Method: `public bool RequiresAnnualVerification(DateTime utcNow)`
    - Returns `true` when `LastVerifiedOnUtc == null` or `LastVerifiedOnUtc.Value <= LectorVerificationPolicy.GetVerificationExpirationCutoffUtc(utcNow)`.

- **New Policy — `LectorVerificationPolicy`** (`Guts.Domain/UserAggregate/LectorVerificationPolicy.cs`):
  - `public static DateTime GetVerificationExpirationCutoffUtc(DateTime utcNow)` ➔ returns `NormalizeToUtc(utcNow).AddYears(-1)`.
  - `public static bool IsLocked(User user, IReadOnlyCollection<string> roles, DateTime utcNow)`:
    - Returns `false` if `user is null` or `roles is null` or `!roles.Contains(Role.Constants.Lector)`.
    - Returns `user.RequiresAnnualVerification(utcNow)`.
  - `public static void EnsureCanUnlock(User targetUser, IReadOnlyCollection<string> targetRoles, DateTime utcNow)`:
    - `Contracts.Require(targetUser is not null, "Target user cannot be null.");`
    - `Contracts.Require(targetRoles != null && targetRoles.Contains(Role.Constants.Lector), "Only teachers (lectors) can be unlocked.");`
    - `Contracts.Require(targetUser.RequiresAnnualVerification(utcNow), "Lector account is already verified and does not need unlocking.");`

---

## 3. Application Layer (`Guts.Application`) — Use-Case Orchestration Only

> [!IMPORTANT]
> Zero domain logic in `Guts.Application`. All lock/unlock decisions delegate to `User` and `LectorVerificationPolicy`.

### Communication (`Guts.Application/Communication/`)
- **`IMailSender`** & **`MailSender`**:
  - Add `Task SendLectorVerificationEmailMessageAsync(User user, string verificationToken);`
  - Implementation delegates to refactored shared `SendUserTokenMailAsync(...)` helper with optional `greeting` (e.g. `"Dear lector,"`), subject `"Verify your PXL lector account"`, callback path `"verify-lector"`, and lector-specific explanation text.

### Repository Contracts (`Guts.Application/Repositories/IUserRepository.cs`)
- Add overload:
  ```csharp
  Task<PagedResult<UserWithRolesAndLinks>> SearchUsersAsync(
      string search,
      string roleFilter,
      bool orphanedOnly,
      bool lockedOnly,
      int page = 1,
      int? pageSize = 50);
  ```
- Add persistence method:
  - `Task UpdateAsync(User user);`
- Preserve existing 5-parameter and 6-parameter `SearchUsersAsync` signatures delegating with `lockedOnly: false`.

### Application Services & DTOs (`Guts.Application/Services/`, `Guts.Application/Dtos/`)
- **`AdminUserDto`**:
  - `public DateTime? LastVerifiedOnUtc { get; set; }`
  - `public bool IsLockedLector { get; set; }`
  - `public bool CanUnlockLector { get; set; }`
- **`IAdminUserService`** & **`AdminUserService`**:
  - Update `SearchUsersAsync(string search, string roleFilter, bool orphanedOnly, bool lockedOnly, int currentUserId, int page = 1, int? pageSize = null)` (computing `IsLockedLector = LectorVerificationPolicy.IsLocked(entry.User, entry.Roles, utcNow)` and `CanUnlockLector = isLocked`).
  - Add `Task UnlockLectorAsync(int targetUserId)`:
    1. Load `targetUser` via `userRepository.GetByIdAsync(targetUserId)` or throw `DataNotFoundException`.
    2. Load `targetRoles` via `userRepository.GetRolesAsync(targetUser)`.
    3. Call `LectorVerificationPolicy.EnsureCanUnlock(targetUser, targetRoles, utcNow)`.
    4. Call `targetUser.MarkEmailVerified(utcNow)`.
    5. Persist via `await userRepository.UpdateAsync(targetUser);`.

---

## 4. Infrastructure Layer (`Guts.Infrastructure`)

### EF Core Configuration & Migrations
- **`User.LastVerifiedOnUtc`**: Mapped automatically by EF Core as nullable `DateTime?` column on `AspNetUsers` (no default value; existing rows remain `NULL` so existing lectors require re-verification at next login).
- **Migration Required?**: `Yes`
  - Migration name: `AddUserLastVerifiedOnUtc`

### Repository Implementations (`Guts.Infrastructure/Repositories/UserDbRepository.cs`)
- Add private filter helper `FilterLockedLectors(IQueryable<User> query, bool lockedOnly, DateTime cutoffUtc)`:
  - When `lockedOnly` is `true`, filters to users who hold `Role.Constants.Lector` AND satisfy `u.LastVerifiedOnUtc == null || u.LastVerifiedOnUtc <= cutoffUtc` (using `LectorVerificationPolicy.GetVerificationExpirationCutoffUtc(DateTime.UtcNow)`).
- Add `UpdateAsync(User user)`:
  - Attaches/marks `user` modified on `_context.Users` and calls `await _context.SaveChangesAsync();`.

---

## 5. API Layer (`Guts.Api`)

### Request/Response Models (`Guts.Api/Models/`)
- Define `VerifyLectorModel` (`UserId`, `Token`, `[Required]`) for `POST /api/auth/verifylector`.
- Define `ResendLectorVerificationModel` (`UserId`, `ResendToken`, `[Required]`) for `POST /api/auth/resend-lector-verification`.
- Define `LectorVerificationRequiredModel` (`Code = "LectorVerificationRequired"`, `UserId`, `Email`, `ResendToken`, `Message`) for the `403 Forbidden` response body on locked-lector login.

### Controllers (`Guts.Api/Controllers/`)
- **`AuthController`**:
  - Constants `internal const string LectorVerificationTokenPurpose = "LectorVerification";` and `internal const string LectorVerificationResendTokenPurpose = "LectorVerificationResend";`
  - `ConfirmEmail`: When `userManager.ConfirmEmailAsync` succeeds, call `user.MarkEmailVerified(DateTime.UtcNow)` and `await userManager.UpdateAsync(user)`.
  - `ResetPassword`: When `userManager.ResetPasswordAsync` succeeds, call `user.MarkEmailVerified(DateTime.UtcNow)` and `await userManager.UpdateAsync(user)`.
  - `CreateToken`: After password check and `EmailConfirmed` check, load `userRoles = await userManager.GetRolesAsync(user)`. If `LectorVerificationPolicy.IsLocked(user, userRoles, DateTime.UtcNow)`:
    - Generate verification token with `userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, LectorVerificationTokenPurpose)`.
    - Generate resend proof token with `userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, LectorVerificationResendTokenPurpose)`.
    - Send email via `mailSender.SendLectorVerificationEmailMessageAsync(user, token)`.
    - Return `StatusCode((int)HttpStatusCode.Forbidden, new LectorVerificationRequiredModel(...))`.
  - `[HttpPost("resend-lector-verification")]`, `[AllowAnonymous]`:
    - Validate `ModelState`; find user (`null` ➔ `200 OK` without revealing user existence).
    - Verify resend token with `userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, LectorVerificationResendTokenPurpose, model.ResendToken)`.
    - If invalid ➔ add error to `ModelState` and return `BadRequest(ModelState)`.
    - If still locked (`LectorVerificationPolicy.IsLocked(user, userRoles, DateTime.UtcNow)`), generate a fresh `LectorVerification` token and send email via `mailSender.SendLectorVerificationEmailMessageAsync(user, token)`; return `200 OK`.
  - `[HttpPost("verifylector")]`, `[AllowAnonymous]`:
    - Validate `ModelState`; find user (`null` ➔ `200 OK` without leaking user existence).
    - Verify token with `userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, LectorVerificationTokenPurpose, model.Token)`.
    - If invalid ➔ add Identity/invalid-token error to `ModelState` and return `BadRequest(ModelState)`.
    - If valid ➔ confirm `user.EmailConfirmed = true`, call `user.MarkEmailVerified(DateTime.UtcNow)`, persist with `userManager.UpdateAsync(user)`, return `200 OK` (or `400 BadRequest` on Identity update error).

- **`AdminUserController`** (`[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]`):
  - Update `SearchUsers`: accept `[FromQuery] bool lockedOnly = false` and pass to `adminUserService.SearchUsersAsync(...)`.
  - Add `[HttpPost("{userId}/unlock")]`: validate `userId >= 1`, invoke `adminUserService.UnlockLectorAsync(userId)`, return `204 NoContent` (`404 Not Found` on `DataNotFoundException`, `400 BadRequest` on `ContractException`).

---

## 6. Frontend Layer (`Guts.Web/ClientApp`)

### ViewModels & Services (`src/app/viewmodels/`, `src/app/services/`, `src/app/util/`)
- `VerifyLectorModel` (`src/app/viewmodels/verify-lector.model.ts`)
- `ResendLectorVerificationModel` (`src/app/viewmodels/resend-lector-verification.model.ts`)
- `LoginResult` (`src/app/util/result.ts`): extends `PostResult` with `requiresLectorVerification?: boolean`, `verificationUserId?: number`, `verificationEmail?: string`, and `resendToken?: string`.
- `AuthService.login(model)`: detects `403` with `error.code === "LectorVerificationRequired"` and returns `LoginResult` (`requiresLectorVerification: true`, `verificationUserId`, `verificationEmail`, `resendToken`).
- `AuthService.verifyLector(model: VerifyLectorModel)`: posts to `api/auth/verifylector`.
- `AuthService.resendLectorVerification(model: ResendLectorVerificationModel)`: posts to `api/auth/resend-lector-verification`.
- `AdminUser` (`src/app/viewmodels/admin-user.model.ts`): adds `lastVerifiedOnUtc: string | null`, `isLockedLector: boolean`, `canUnlockLector: boolean`.
- `AdminUserService`: adds optional `lockedOnly` query param on `searchUsers(...)` and `unlockLector(userId: number): Observable<void>` posting to `api/admin/users/${userId}/unlock`.

### Components & Routing (`src/app/components/`, `src/app/app-routing.module.ts`, `src/app/app.module.ts`)
- **`LoginComponent`**: Navigates to `["/lector-verification-sent"]` with `{ state: { email: result.verificationEmail ?? this.model.email, userId: result.verificationUserId, resendToken: result.resendToken } }` when `result.requiresLectorVerification` is true.
- **`LectorVerificationSentComponent`** (`/lector-verification-sent`, public route): Displays email-sent notice (with `email` from navigation state when present), explains account access is locked until email confirmation, renders a **"Resend verification email"** button when `userId` and `resendToken` are available (calling `authService.resendLectorVerification(...)`), and links back to `/login`.
- **`VerifyLectorComponent`** (`/verify-lector`, public route): Reads `userId` and `token` query params, invokes `authService.verifyLector(...)`, renders loading / unlocked success + **"Log in"** button to `/login` / invalid-token error + **"Back to Login"** link.
- **`AdminUsersComponent`** (`/admin/users`):
  - Adds **"Locked only"** switch toggle alongside **"Orphaned only"**.
  - Adds **Verification** column showing `Locked` / `Verified` badge (with `LastVerifiedOnUtc` tooltip) for `lector` rows (`—` for non-lectors).
  - Adds **"Unlock"** action button on locked lectors opening a `<dialog open>` confirmation modal before calling `adminUserService.unlockLector(user.id)`.

---

## 7. Dependency Injection (`Guts.Bootstrapper`)

- All modified services and repositories (`AdminUserService`, `UserDbRepository`, `MailSender`) retain their existing `*Service` / `*Repository` names and constructors registered automatically by `StartUpExtensions.cs`.
- `LectorVerificationPolicy` is a stateless static domain policy (no DI registration needed).
