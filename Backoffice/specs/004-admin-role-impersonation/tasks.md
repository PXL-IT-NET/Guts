# TDD Implementation Plan: Admin Role, Period & Course Management, User Cleanup, and Impersonation

- **Feature ID**: `004-admin-role-impersonation`
- **Specification**: [`spec.md`](./spec.md)
- **Technical Design**: [`design.md`](./design.md)

> **Instructions**: Follow Test-Driven Development (TDD) strictly. For each slice, write or update the failing unit test first, implement the production code to make it pass, and mark the checkbox (`[x]`) once verified.

---

## Phase 1: Domain Layer (`Guts.Domain.Tests` ➔ `Guts.Domain`)

- [x] **[T1.1]** `[AC-1]` Add `Role.Constants.Admin = "admin"` in `Guts.Domain/RoleAggregate/Role.cs`.
- [x] **[T1.2]** `[AC-2, DR-2]` Write failing unit tests in `Guts.Domain.Tests/PeriodAggregate/PeriodTests.cs` for `EnsureCanBeDeleted()` (succeeds for past non-active periods; throws `ContractException` for active or future periods).
- [x] **[T1.3]** `[AC-2, DR-2]` Implement `EnsureCanBeDeleted()` on `IPeriod` and `Period` in `Guts.Domain/PeriodAggregate/`.
- [x] **[T1.4]** `[AC-3, DR-3]` Write failing unit tests in `Guts.Domain.Tests/CourseAggregate/CourseTests.cs` for `Course.Factory.CreateNew(...)` (including duplicate `Code` check) and `Course.Update(...)` (non-empty `Code` <= 20 chars, non-empty `Name`).
- [x] **[T1.5]** `[AC-3, DR-3]` Implement `ICourseFactory` / `Course.Factory` and `Course.Update(...)` in `Guts.Domain/CourseAggregate/Course.cs`.
- [x] **[T1.6]** `[AC-4, DR-4]` Write failing unit tests in `Guts.Domain.Tests/UserAggregate/UserDeletionPolicyTests.cs` and `UserCourseLinksTests.cs` covering all deletion eligibility rules (self, admin role, any course link, Orphaned non-admin).
- [x] **[T1.7]** `[AC-4, DR-4]` Implement `UserCourseLinks` Value Object and `UserDeletionPolicy` in `Guts.Domain/UserAggregate/`.
- [x] **[T1.8]** `[AC-5, DR-5]` Write failing unit tests in `Guts.Domain.Tests/UserAggregate/ImpersonationPolicyTests.cs` covering admin impersonating lector/student, lector impersonating student only, blocking lector impersonating lector/admin, blocking admin-on-admin, and blocking nested impersonation.
- [x] **[T1.9]** `[AC-5, DR-5]` Implement `ImpersonationPolicy` in `Guts.Domain/UserAggregate/`.
- [x] **[T1.10]** Run `dotnet test Backoffice/Guts.Domain.Tests/Guts.Domain.Tests.csproj` and verify all tests pass.

---

## Phase 2: Application Layer (`Guts.Application.Tests` ➔ `Guts.Application`)

- [x] **[T2.1]** `[AC-5]` Add `GutsClaimTypes.ImpersonatorId` and write failing unit tests in `Guts.Application.Tests/Security/JwtSecurityTokenAccessPassFactoryTests.cs` for optional `impersonatorId` claim inclusion; implement in `JwtSecurityTokenAccessPassFactory`.
- [x] **[T2.2]** `[AC-2, DR-2]` Write failing unit tests in `Guts.Application.Tests/Services/Period/PeriodServiceTests.cs` for `DeletePeriodAsync`; implement `DeletePeriodAsync` in `IPeriodService` / `PeriodService` (load ➔ `period.EnsureCanBeDeleted()` ➔ `repository.DeleteAsync`).
- [x] **[T2.3]** `[AC-3, DR-3]` Write failing unit tests in `Guts.Application.Tests/Services/CourseServiceTests.cs` for `CreateCourseAsync`, `UpdateCourseAsync`, and `DeleteCourseAsync`; implement in `ICourseService` / `CourseService`.
- [x] **[T2.4]** `[AC-4, DR-4]` Define `IUserRepository` extensions (`SearchUsersAsync`, `GetCourseLinksAsync`, `GetOrphanedNonAdminUsersAsync`), `AdminUserDto`, and `IAdminUserService`; write failing unit tests in `Guts.Application.Tests/Services/AdminUserServiceTests.cs` and implement `AdminUserService`.
- [x] **[T2.5]** `[AC-5, DR-5]` Define `IImpersonationService` and `ImpersonatableUserDto`; write failing unit tests in `Guts.Application.Tests/Services/ImpersonationServiceTests.cs` and implement `ImpersonationService`.
- [x] **[T2.6]** Run `dotnet test Backoffice/Guts.Application.Tests/Guts.Application.Tests.csproj` and verify all tests pass.

---

## Phase 3: Infrastructure Layer (`Guts.Infrastructure.Tests` ➔ `Guts.Infrastructure`)

- [x] **[T3.1]** `[AC-1]` Update `GutsDbInitializerTests.cs` and `GutsDbInitializer.cs` to create each role (`student`, `lector`, `admin`) individually if missing and assign `admin` role to configured `AdminEmails`.
- [x] **[T3.2]** `[AC-2]` Write failing integration/repository tests in `Guts.Infrastructure.Tests/Repositories/PeriodDbRepositoryTests.cs` verifying `DeleteAsync` cascades cleanly to `StudentCourseProfile` (and its `Badges`/`Events`), `TestResult`, `TestRun`, `SolutionFile`, `Chapter`, `Project`, `CourseClassGroup`, and assessments inside a transaction; implement in `PeriodDbRepository`.
- [x] **[T3.3]** `[AC-3]` Write failing repository tests in `Guts.Infrastructure.Tests/Repositories/CourseDbRepositoryTests.cs` verifying `DeleteAsync` cascades cleanly to `StudentCourseProfile` and all dependent course entities; implement in `CourseDbRepository`.
- [x] **[T3.4]** `[AC-4]` Write failing repository tests in `Guts.Infrastructure.Tests/Repositories/UserDbRepositoryTests.cs` for `GetCourseLinksAsync`, `SearchUsersAsync`, and `GetOrphanedNonAdminUsersAsync`; implement in `UserDbRepository`.
- [x] **[T3.5]** Run `dotnet test Backoffice/Guts.Infrastructure.Tests/Guts.Infrastructure.Tests.csproj` and verify all tests pass.

---

## Phase 4: API Layer (`Guts.Api.Tests` ➔ `Guts.Api`)

- [x] **[T4.1]** `[AC-1, AC-5]` Add `AdminOnlyPolicy` and `LectorOrAdminPolicy` in `ApiConstants.cs` and `Startup.cs`; add `IsAdmin()` and `IsImpersonating()` to `ControllerBase` (with unit tests in `ControllerBaseTests.cs`).
- [x] **[T4.2]** `[AC-5, DR-5]` Write failing unit tests in `Guts.Api.Tests/Filters/ImpersonationReadOnlyFilterTests.cs` verifying `POST`/`PUT`/`PATCH`/`DELETE` requests with `impersonator_id` claim return `403 Forbidden` while `GET` requests pass; implement and register `ImpersonationReadOnlyFilter` in `Startup.cs`.
- [x] **[T4.3]** `[AC-5]` Update `UserController` & `UserProfileModel` to include `IsImpersonating` (with tests in `UserControllerTests.cs`).
- [x] **[T4.4]** `[AC-2]` Write failing unit tests in `Guts.Api.Tests/Controllers/PeriodControllerTests.cs` for `[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]` on `CreatePeriod`, `UpdatePeriod` (`PUT`), and `DeletePeriod` (`DELETE`); implement in `PeriodController`.
- [x] **[T4.5]** `[AC-3]` Write failing unit tests in `Guts.Api.Tests/Controllers/CourseControllerTests.cs` for `CreateCourse` (`POST`), `UpdateCourse` (`PUT`), and `DeleteCourse` (`DELETE`) with `AdminOnlyPolicy`; implement in `CourseController`.
- [x] **[T4.6]** `[AC-4]` Write failing unit tests in `Guts.Api.Tests/Controllers/AdminUserControllerTests.cs` for `GET /api/admin/users`, `DELETE /api/admin/users/{userId}`, and `DELETE /api/admin/users/Orphaned` (`AdminOnlyPolicy`); implement `AdminUserController`.
- [x] **[T4.7]** `[AC-5]` Write failing unit tests in `Guts.Api.Tests/Controllers/ImpersonationControllerTests.cs` for `GET /api/impersonation/users` and `POST /api/impersonation/{userId}` (`LectorOrAdminPolicy`); implement `ImpersonationController`.
- [x] **[T4.8]** Run `dotnet test Backoffice/Guts.Api.Tests/Guts.Api.Tests.csproj` and `dotnet test Backoffice/Guts.Architecture.Tests/Guts.Architecture.Tests.csproj` and verify all tests pass.

---

## Phase 5: Frontend SPA (`Guts.Web/ClientApp`)

- [x] **[T5.1]** `[AC-1, AC-5, AC-6]` Update `UserProfile` (`isAdmin()`, `isImpersonating`), `LocalStorageKeys`, and `AuthService` (`startImpersonation`, `stopImpersonation`, `isImpersonating`) with Jasmine unit tests.
- [x] **[T5.2]** `[AC-5, DR-5]` Implement `impersonationReadOnlyInterceptor` (blocking non-GET/HEAD/OPTIONS requests while impersonating) and persistent impersonation read-only banner + **Stop Impersonating** button in `app.component` (with unit tests).
- [x] **[T5.3]** `[AC-6]` Implement `adminGuard`, `lectorOrAdminGuard`, and **Admin** / **Impersonate Student** sidebar menu entries in `sidebar-menu.component` (with unit tests).
- [x] **[T5.4]** `[AC-2]` Extend `PeriodService` and implement `AdminPeriodsComponent` (CRUD + dangerous delete confirmation warning for past non-active periods) with `*.spec.ts`.
- [x] **[T5.5]** `[AC-3]` Extend `CourseService` and implement `AdminCoursesComponent` (CRUD + dangerous delete confirmation warning) with `*.spec.ts`.
- [x] **[T5.6]** `[AC-4, AC-5]` Implement `AdminUserService`, `ImpersonationService`, `AdminUsersComponent` (search/filter, impersonate non-admin, single/bulk delete of Orphaned users with **extra teacher warning** when a `lector` is about to be deleted), and `ImpersonateStudentComponent` (for lectors) with `*.spec.ts`.
- [x] **[T5.7]** Run `npm test -- --watch=false` and `npm run lint` in `Backoffice/Guts.Web/ClientApp` and verify all tests and lint checks pass.

---

## Phase 6: Final Verification & Quality Gate

- [x] **[T6.1]** Run full backend test suite: `dotnet test Backoffice/Guts.slnx`
- [x] **[T6.2]** Run frontend production build: `npm run build` (from `Backoffice/Guts.Web/ClientApp`)
- [ ] **[T6.3]** Run SonarQube analysis (`.\run-sonarqube.ps1 -UseSonarCloud` from `Backoffice/`) and resolve any reported bugs, code smells, or coverage gaps.
- [x] **[T6.4]** Update `spec.md` and `design.md` status to `Implemented`.
