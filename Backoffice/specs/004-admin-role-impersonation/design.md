# Technical Design: Admin Role, Period & Course Management, User Cleanup, and Impersonation

- **Feature ID**: `004-admin-role-impersonation`
- **Specification**: [`spec.md`](./spec.md)
- **Status**: `Implemented`

---

## 1. Architectural Overview

This feature spans all layers of the GUTS Backoffice:
- **`Guts.Domain`**: Adds `Role.Constants.Admin`, enforces period deletion invariants (`EnsureCanBeDeleted()`), adds `Course` update/uniqueness validation and `ICourseFactory`, and models impersonation eligibility and user deletion eligibility as pure domain policies/rules (`ImpersonationPolicy`, `UserDeletionPolicy` or `User` domain methods).
- **`Guts.Application`**: Thin use-case orchestration for Period CRUD (`IPeriodService`), Course CRUD (`ICourseService`), User search & Orphaned cleanup (`IAdminUserService`), and Impersonation (`IImpersonationService`). Zero domain logic in services—all business rules are delegated to `Guts.Domain`.
- **`Guts.Infrastructure`**: Updates `GutsDbInitializer` to seed the `admin` role individually and assign configured initial admin emails; implements `UserDbRepository` extensions for searching users and checking course links; implements transactional cascade cleanup in `PeriodDbRepository` and `CourseDbRepository` where EF Core uses `Restrict`/`NoAction`.
- **`Guts.Api`**: Adds `AdminOnlyPolicy` and `LectorOrAdminPolicy` in `Startup.cs` & `ApiConstants.cs`; secures `PeriodController` and `CourseController` CRUD endpoints; adds `AdminUserController` (`/api/admin/users`) and `ImpersonationController` (`/api/impersonation`).
- **`Guts.Web/ClientApp`**: Adds `UserProfile.isAdmin()` / `isImpersonating`, `AdminGuard` & `LectorOrAdminGuard`, `AuthService` impersonation token stack + `StopImpersonating`, persistent impersonation banner in `app.component.html`, **Admin** & **Impersonate Student** sidebar links, and Angular components for Period management, Course management, and User search/cleanup/impersonation.

---

## 2. Domain Layer (`Guts.Domain`)

### Aggregates, Entities & Domain Services
- **`Role`** (`Guts.Domain/RoleAggregate/Role.cs`):
  - Add `public const string Admin = "admin";` to `Role.Constants`.
- **`Period`** (`Guts.Domain/PeriodAggregate/Period.cs` & `IPeriod.cs`):
  - Add `void EnsureCanBeDeleted()` (and/or `bool CanBeDeleted()`):
    - Enforces **[DR-2]**: `Contracts.Require(!IsActive, "An active period cannot be deleted.");`
- **`Course`** (`Guts.Domain/CourseAggregate/Course.cs`):
  - Add `Update(string code, string name)`:
    - Enforces **[DR-3]**: `Contracts.Require(!string.IsNullOrWhiteSpace(code), "Course code cannot be empty.");`, `Contracts.Require(code.Trim().Length <= 20, "Course code cannot exceed 20 characters.");`, `Contracts.Require(!string.IsNullOrWhiteSpace(name), "Course name cannot be empty.");`.
    - Updates `Code = code.Trim(); Name = name.Trim();`.
- **Domain Policies (`Guts.Domain/UserAggregate/`)**:
  - **`UserDeletionPolicy`** (pure domain service / static or `IUserDeletionPolicy`):
    - `EnsureCanBeDeleted(User targetUser, int currentUserId, IReadOnlyCollection<string> targetRoles, UserCourseLinks links)`:
      - Enforces **[DR-4]**:
        - `Contracts.Require(targetUser.Id != currentUserId, "You cannot delete your own account.");`
        - `Contracts.Require(!targetRoles.Contains(Role.Constants.Admin), "Administrator accounts cannot be deleted.");`
        - `Contracts.Require(!links.HasAnyLinks, "User cannot be deleted because they are still linked to course data.");`
  - **`UserCourseLinks`** (immutable Value Object in `Guts.Domain/UserAggregate/UserCourseLinks.cs`):
    - Properties (read-only): `HasTestRuns`, `HasTestResults`, `HasSolutionFiles`, `HasProjectTeamMemberships`, `HasStudentCourseProfiles`, `HasPeerAssessments`.
    - Computed: `bool HasAnyLinks => HasTestRuns || HasTestResults || HasSolutionFiles || HasProjectTeamMemberships || HasStudentCourseProfiles || HasPeerAssessments;`
  - **`AdminRolePolicy`** (pure domain service in `Guts.Domain/UserAggregate/AdminRolePolicy.cs`):
    - `EnsureCanPromoteToAdmin(IReadOnlyCollection<string> targetRoles)`: enforces **[DR-6]** (`targetRoles.Contains(Role.Constants.Lector)`).
    - `EnsureCanDemoteFromAdmin(IReadOnlyCollection<string> targetRoles, int currentAdminCount)`: enforces **[DR-6]** (`targetRoles.Contains(Role.Constants.Admin)` and `currentAdminCount > 1`).
  - **`ImpersonationPolicy`** (pure domain service in `Guts.Domain/UserAggregate/ImpersonationPolicy.cs`):
    - `EnsureCanImpersonate(IReadOnlyCollection<string> callerRoles, bool isAlreadyImpersonating, User targetUser, IReadOnlyCollection<string> targetRoles)`:
      - Enforces **[DR-5]**:
        - `Contracts.Require(!isAlreadyImpersonating, "Nested impersonation is not allowed.");`
        - `Contracts.Require(callerRoles.Contains(Role.Constants.Admin) || callerRoles.Contains(Role.Constants.Lector), "Only administrators or lectors can impersonate users.");`
        - `Contracts.Require(!targetRoles.Contains(Role.Constants.Admin), "Administrator accounts cannot be impersonated.");`
        - If `!callerRoles.Contains(Role.Constants.Admin)` (i.e. caller is a lector-only):
          - `Contracts.Require(targetRoles.Contains(Role.Constants.Student) && !targetRoles.Contains(Role.Constants.Lector), "Lectors can only impersonate student accounts.");`

### Domain Factories
- **`ICourseFactory` / `Course.Factory`** (`Guts.Domain/CourseAggregate/`):
  - `Course CreateNew(string code, string name, IReadOnlyCollection<Course> existingCourses)`:
    - Enforces **[DR-3]**: validates non-empty code/name, max length 20, and checks no existing course has the same `Code` (case-insensitive).

---

## 3. Application Layer (`Guts.Application`) — Use-Case Orchestration Only

> [!IMPORTANT]
> Zero domain logic in `Guts.Application`. Services load data from repositories, call `Guts.Domain` methods/factories/policies, and persist results.

### Security (`Guts.Application/Security/`)
- **`GutsClaimTypes`**: Define constant `public const string ImpersonatorId = "impersonator_id";`.
- **`ITokenAccessPassFactory` / `JwtSecurityTokenAccessPassFactory`**:
  - Add overload or optional parameter `int? impersonatorId = null` to `Create(User user, IList<Claim> currentUserClaims, IList<string> userRoles, int? impersonatorId = null)`.
  - When `impersonatorId.HasValue`, include `new Claim(GutsClaimTypes.ImpersonatorId, impersonatorId.Value.ToString())`.

### Repository Contracts (`Guts.Application/Repositories/`)
- **`IPeriodRepository`**:
  - Ensure `DeleteAsync(IPeriod period)` (inherited or explicit) removes the period and any dependent records cleanly.
- **`ICourseRepository`**:
  - Ensure `DeleteAsync(Course course)` removes the course and any dependent records cleanly.
- **`IUserRepository`**:
  - `Task<PagedResult<UserWithRolesAndLinks>> SearchUsersAsync(string search, string roleFilter, bool orphanedOnly, int page = 1, int? pageSize = 50);` (uses EF Core `Skip`/`Take` when `pageSize` has a value, returns all when `pageSize` is `null`, and hydrates roles and linked courses).
  - `Task<UserCourseLinks> GetCourseLinksAsync(int userId);`
  - `Task<IReadOnlyList<UserWithRolesAndLinks>> GetWithRolesAndLinksByIdsAsync(IReadOnlyCollection<int> userIds);`
  - `Task DeleteRangeAsync(IReadOnlyCollection<User> users);`

### Application Services (`Guts.Application/Services/`)
- **`IPeriodService` / `PeriodService`**:
  - Add `Task DeletePeriodAsync(int periodId);`:
    - Load period via `repository.GetPeriodAsync(periodId)` ➔ call `period.EnsureCanBeDeleted()` ➔ `await repository.DeleteAsync(period)`.
- **`ICourseService` / `CourseService`**:
  - Add `Task<Course> CreateCourseAsync(string code, string name);` (loads all courses, calls `courseFactory.CreateNew(...)`, saves).
  - Add `Task<Course> UpdateCourseAsync(int courseId, string code, string name);` (loads course + checks duplicate code via domain, calls `course.Update(...)`, updates).
  - Add `Task DeleteCourseAsync(int courseId);` (loads course, calls `courseRepository.DeleteAsync(course)`).
- **`IAdminUserService` / `AdminUserService`**:
  - `Task<PagedResult<AdminUserDto>> SearchUsersAsync(string search, string role, bool orphanedOnly, int currentUserId, int page = 1, int? pageSize = null);`
  - `Task DeleteUserAsync(int targetUserId, int currentUserId);` (loads user + roles + `UserCourseLinks`, calls `UserDeletionPolicy.EnsureCanBeDeleted(...)`, deletes via `IUserRepository.DeleteAsync`).
  - `Task<int> DeleteUsersAsync(IReadOnlyCollection<int> targetUserIds, int currentUserId);` (loads selected users with roles + links, validates every user via `UserDeletionPolicy.EnsureCanBeDeleted(...)`, deletes via `IUserRepository.DeleteRangeAsync`).
- **`IImpersonationService` / `ImpersonationService`**:
  - `Task<IReadOnlyList<ImpersonatableUserDto>> SearchImpersonatableUsersAsync(string search, IReadOnlyCollection<string> callerRoles);`
  - `Task<TokenAccessPass> ImpersonateAsync(int callerUserId, IReadOnlyCollection<string> callerRoles, bool isAlreadyImpersonating, int targetUserId);`
    - Loads `targetUser` and `targetRoles`, calls `ImpersonationPolicy.EnsureCanImpersonate(...)`, generates `TokenAccessPass` with `impersonatorId: callerUserId`.

---

## 4. Infrastructure Layer (`Guts.Infrastructure`)

### EF Core Configuration & Migrations
- **Migration Required?**: `No` — `Role` uses existing `AspNetRoles` table (`IdentityRole<int>`).
- **`GutsDbInitializer`** (`Guts.Infrastructure/GutsDbInitializer.cs`):
  - Check each role individually (`Student`, `Lector`, `Admin`) instead of returning early if `Student` exists.
  - Read optional `AdminEmails` (`string[]`) from `IConfiguration` (or seed default admin in dev) and ensure those users are assigned `Role.Constants.Admin`.

### Repository Implementations (`Guts.Infrastructure/Repositories/`)
- **`PeriodDbRepository`**:
  - Implement safe deletion of a past `Period` inside `ExecuteInTransactionAsync` using `ExecuteDeleteAsync()`: explicitly delete `CourseClassGroup` rows for `PeriodId` (which use `DeleteBehavior.NoAction` to avoid multiple cascade paths in SQL Server), then delete the `Period`, letting database `ON DELETE CASCADE` remove `StudentCourseProfile` (with `StudentBadges` and `GamificationEvents`) and `Topic` hierarchies.
- **`CourseDbRepository`**:
  - Implement safe deletion of a `Course` inside `ExecuteInTransactionAsync` using `ExecuteDeleteAsync()`: explicitly delete `CourseClassGroup` rows for `CourseId` (which use `DeleteBehavior.NoAction` to avoid multiple cascade paths in SQL Server), then delete the `Course`, letting database `ON DELETE CASCADE` remove `StudentCourseProfile` (with `StudentBadges` and `GamificationEvents`), `CourseGamificationConfig`, and `Topic` hierarchies.
- **`UserDbRepository`**:
  - Implement `GetCourseLinksAsync(int userId)`, `GetWithRolesAndLinksByIdsAsync(...)`, `DeleteRangeAsync(...)`, and paginated `SearchUsersAsync(...)` using EF Core `Skip`/`Take` and hydrating roles and linked courses (`LinkedCourseDto`) across `TestRuns`, `TestResults`, `SolutionFiles`, `ProjectTeamUsers`, `StudentCourseProfiles`, and `PeerAssessments`.

---

## 5. API Layer (`Guts.Api`)

### Authorization Policies (`ApiConstants.cs` & `Startup.cs`)
- `AdminOnlyPolicy`: `RequireAuthenticatedUser().RequireRole(Role.Constants.Admin)`
- `LectorOrAdminPolicy`: `RequireAuthenticatedUser().RequireRole(Role.Constants.Admin, Role.Constants.Lector)`
- `LectorsOnlyPolicy`: Unchanged (`RequireRole(Role.Constants.Lector)`).

### Filters (`Guts.Api/Filters/`)
- **`ImpersonationReadOnlyFilter`** (global `IAsyncActionFilter` registered in `Startup.cs`):
  - Checks if `User.HasClaim(c => c.Type == GutsClaimTypes.ImpersonatorId)` and `HttpMethods.IsPost/Put/Patch/Delete(request.Method)`.
  - If so, short-circuits with `403 Forbidden` (`ObjectResult(new ErrorModel("Mutations are not allowed while impersonating a user.")) { StatusCode = StatusCodes.Status403Forbidden }`).

### Controllers (`Guts.Api/Controllers/`)
- **`ControllerBase`**:
  - Add `protected bool IsAdmin() => User?.IsInRole(Role.Constants.Admin) ?? false;`
  - Add `protected bool IsImpersonating() => User?.HasClaim(c => c.Type == GutsClaimTypes.ImpersonatorId) ?? false;`
- **`UserController`**:
  - `GET /api/users/current/profile`: Include `IsImpersonating` (and `ImpersonatorId` if present) on `UserProfileModel`.
- **`PeriodController`**:
  - Add `[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]` to `POST /api/periods`.
  - Add `[HttpPut("{id}")]` (`AdminOnlyPolicy`) ➔ `periodService.UpdatePeriodAsync(...)`.
  - Add `[HttpDelete("{id}")]` (`AdminOnlyPolicy`) ➔ `periodService.DeletePeriodAsync(id)`.
- **`CourseController`**:
  - Add `[HttpPost]` (`AdminOnlyPolicy`) ➔ `courseService.CreateCourseAsync(...)`.
  - Add `[HttpPut("{courseId}")]` (`AdminOnlyPolicy`) ➔ `courseService.UpdateCourseAsync(...)`.
  - Add `[HttpDelete("{courseId}")]` (`AdminOnlyPolicy`) ➔ `courseService.DeleteCourseAsync(courseId)`.
- **`AdminUserController`** (`[Route("api/admin/users")]`, `[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]`):
  - `GET /api/admin/users?search=...&role=...&orphanedOnly=...&page=...&pageSize=...`
  - `DELETE /api/admin/users/{userId}`
  - `POST /api/admin/users/delete-batch`
- **`ImpersonationController`** (`[Route("api/impersonation")]`, `[Authorize(Policy = ApiConstants.LectorOrAdminPolicy)]`):
  - `GET /api/impersonation/users?search=...`
  - `POST /api/impersonation/{userId}`

---

## 6. Frontend Layer (`Guts.Web/ClientApp`)

### Services, ViewModels & Guards
- **`UserProfile`** (`src/app/viewmodels/user.model.ts`):
  - Add `isImpersonating?: boolean` to `IUserProfile` / `UserProfile`.
  - Add `public isAdmin(): boolean { return this.roles.includes("admin"); }`.
- **`LocalStorageKeys`** (`src/app/util/localstorage.keys.ts`):
  - Add `originalToken = "originalToken"`.
- **`AuthService`** (`src/app/services/auth.service.ts`):
  - Add `startImpersonation(tokenModel: TokenModel)` (saves current `tokenModel` to `localStorage[originalToken]`, sets `currentToken` to impersonation token, invalidates profile cache, emits logged-in/impersonation state).
  - Add `stopImpersonation()` (restores `originalToken` to `currentToken`, removes `originalToken`, invalidates profile cache, reloads).
  - Expose `isImpersonating()` signal/boolean.
- **Guards** (`src/app/guards/`):
  - `adminGuard`: Allows if authenticated and `userProfile.isAdmin()` (and not impersonating); otherwise redirects to `/home`.
  - `lectorOrAdminGuard`: Allows if authenticated and (`userProfile.isAdmin()` || `userProfile.isLector()`) and not impersonating; otherwise redirects to `/home`.
- **Services**:
  - Extend `PeriodService` with `createPeriod`, `updatePeriod`, `deletePeriod`.
  - Extend `CourseService` with `createCourse`, `updateCourse`, `deleteCourse`.
  - Add `AdminUserService` (`/api/admin/users`) and `ImpersonationService` (`/api/impersonation`).

### Components & Routing
- **Persistent Impersonation Banner & Read-Only Mode** (`app.component.html` / `app.component.ts` & `ImpersonationReadOnlyInterceptor`):
  - When `authService.isImpersonating()` is true, render a prominent top alert banner (`alert-warning` / `bg-warning`) showing the impersonated user's email & roles, a **"Read-only mode — changes are disabled"** notice, and a **"Stop Impersonating"** button.
  - Add an HTTP interceptor (`impersonationReadOnlyInterceptor`) that blocks non-GET/HEAD/OPTIONS requests while `authService.isImpersonating()` is true (in addition to the backend `ImpersonationReadOnlyFilter`), and disable interactive mutation controls in the UI when impersonating.
- **Sidebar Menu** (`sidebar-menu.component.html`):
  - `@if (userProfile.isAdmin() && !isImpersonating)`: Collapsible **Admin** section with **Periods** (`/admin/periods`), **Courses** (`/admin/courses`), **Users & Impersonation** (`/admin/users`).
  - `@if (userProfile.isLector() && !userProfile.isAdmin() && !isImpersonating)`: Collapsible **Teacher tools** section (expanded by default) containing **Impersonate student** (`/impersonate`).
- **New Components** (`src/app/components/admin/` or `src/app/components/`):
  - `AdminPeriodsComponent`: Table of periods + Create/Edit form (validates required fields and disables `from`/`until` for inactive periods) + Delete button with centered `DangerDeleteModalComponent` (disabled/blocked if active period; past and future non-active periods can be deleted; requires typing period `description` to confirm).
  - `AdminCoursesComponent`: Table of courses + Create/Edit form + Delete button with centered `DangerDeleteModalComponent` warning about irreversible data loss (requires typing course `code` to confirm).
  - `AdminUsersComponent`: Search input, role filter, "Orphaned only" toggle, table of users with **Impersonate** button (for non-admins) and **Delete** button (for Orphaned non-admins; shows **extra prominent teacher warning** when the target user has `lector` role), plus **Remove All Orphaned Users** button with confirmation modal (including extra warning if any `lector` accounts are included).
  - `ImpersonateStudentComponent` (for lectors): Search input + table of student accounts with **Impersonate** button.

---

## 7. Dependency Injection (`Guts.Bootstrapper`)

- `Course.Factory` (`ICourseFactory`), `Period.Factory` (`IPeriodFactory`) in `Guts.Domain`
- `AdminUserService` (`IAdminUserService`), `ImpersonationService` (`IImpersonationService`) in `Guts.Application` (auto-registered as `*Service` ➔ Scoped)
- `UserDbRepository` (`IUserRepository`), `PeriodDbRepository` (`IPeriodRepository`), `CourseDbRepository` (`ICourseRepository`) in `Guts.Infrastructure` (auto-registered as `*Repository` ➔ Scoped)
- Confirm `Guts.Architecture.Tests` passes for all new types and controller authorization attributes.
