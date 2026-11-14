# Specification: Admin Role, Period & Course Management, User Cleanup, and Impersonation

- **Feature ID**: `004-admin-role-impersonation`
- **Status**: `Implemented`
- **Created**: `2026-11-13`

---

## 1. Context & Problem Statement

Currently, GUTS Backoffice only has two roles: `student` and `lector` (`Role.Constants.Student`, `Role.Constants.Lector`). Operational tasks such as managing academic periods and courses are either unprotected at the API level (`POST /api/periods` lacks a role policy), missing endpoints/UI (period update/delete, course create/update/delete), or lack a dedicated administrative role.

Additionally, administrators and lectors need to:
1. Manage academic periods (full CRUD, with safe deletion of past periods) and courses (full CRUD, with proper warnings on deletion) — **Administrators**.
2. Remove old user accounts that are no longer linked to any course data (test runs, test results, solution files, project teams, gamification profiles, or peer assessments) to keep the user base clean — **Administrators**.
3. Impersonate users so they can inspect the application through that user's eyes for support and troubleshooting, and easily return to their own session — **Administrators** (`lector` or `student`) and **Lectors** (`student`).

---

## 2. User Stories & Actors

- **Actors**: `Administrator` (`admin`), `Lector` (`lector`), `Student` (`student`)

- **US-1**: As an **Administrator**, I want a dedicated `admin` role that can be held alongside `lector` or `student` (and seeded/configured via settings/DB), so that administrative privileges are cleanly separated from teaching and student roles.
- **US-2**: As an **Administrator**, I want to create, view, edit, and delete academic periods (with deletion restricted to non-active periods—past or future—and accompanied by a clear danger warning), so that I can manage the academic calendar safely.
- **US-3**: As an **Administrator**, I want to create, view, edit, and delete courses (with a clear danger warning before deletion), so that I can maintain the course catalog.
- **US-4**: As an **Administrator**, I want to search users and remove old users who are no longer linked to any course data (individually or in bulk), so that obsolete accounts can be cleaned up safely.
- **US-5**: As an **Administrator**, I want to impersonate a `lector` or `student` and see a persistent banner with a one-click "Stop Impersonating" action, so that I can inspect the application through their eyes without needing their credentials.
- **US-6**: As a **Lector**, I want to impersonate a `student` and see the same persistent banner with a one-click "Stop Impersonating" action, so that I can verify what a student sees in my courses without needing their credentials.

---

## 3. Functional Requirements & Acceptance Criteria

### AC-1: Admin Role & Seeding
- **Given** the application starts and `GutsDbInitializer` runs
- **When** roles are initialized
- **Then** `Role.Constants.Admin` (`"admin"`) is created alongside `student` and `lector` (checking each role individually so existing databases also receive `admin`).
- **And** initial admin accounts can be configured (e.g., via `appsettings` / `GutsDbInitializer`) and users may hold multiple roles (e.g., `lector` + `admin`).

### AC-2: Period Management (CRUD)
- **Given** an authenticated user with the `admin` role
- **When** they manage periods via `/api/periods` and the Admin Periods UI
- **Then**:
  - `GET /api/periods` and `GET /api/periods/{id}` remain readable to authenticated users (as needed for period selection across the app).
  - `POST /api/periods` creates a new period (restricted to `AdminOnlyPolicy`).
  - `PUT /api/periods/{id}` updates an existing period's description, `from`, and `until` dates (restricted to `AdminOnlyPolicy`; start and end dates cannot be changed for inactive periods).
  - `DELETE /api/periods/{id}` deletes a period (restricted to `AdminOnlyPolicy`) **only if** the period is not the current/active period (past or future periods are allowed).
  - In the UI, clicking Delete opens a centered dangerous-operation confirmation modal where the admin must type the exact period `description` to enable and execute the delete.

### AC-3: Course Management (CRUD)
- **Given** an authenticated user with the `admin` role
- **When** they manage courses via `/api/courses` and the Admin Courses UI
- **Then**:
  - `GET /api/courses` and `GET /api/courses/{courseId}` remain readable to authenticated users.
  - `POST /api/courses` creates a new course with `code` and `name` (restricted to `AdminOnlyPolicy`).
  - `PUT /api/courses/{courseId}` updates an existing course's `code` and `name` (restricted to `AdminOnlyPolicy`).
  - `DELETE /api/courses/{courseId}` deletes a course and its associated data (restricted to `AdminOnlyPolicy`).
  - In the UI, clicking Delete opens a centered dangerous-operation confirmation modal explaining that deleting the course is irreversible and removes associated course data, requiring the admin to type the exact course `code` to enable and execute the delete.

### AC-4: Paginated User Search, Course Links Display & Multi-Select User Deletion
- **Given** an authenticated user with the `admin` role
- **When** they open the Admin Users view or call `/api/admin/users`
- **Then**:
  - `GET /api/admin/users?search=...&role=...&orphanedOnly=...&page=...&pageSize=...` returns a paged result (`items`, `totalCount`, `page`, `pageSize`) using EF Core `Skip` and `Take` (or returning all matching rows when `pageSize` is omitted / `All`).
  - The UI provides a page size selector supporting `25`, `50`, `100`, and `All`, along with pagination controls and result counts.
  - Each user entry includes `id`, `email`, `firstName`, `lastName`, `roles`, `isLinkedToCourse`, `linkedCourses` (`id`, `code`, `name`), and `canDelete`.
  - The Course Links column displays `"Orphaned"` or `"Linked to x course(s)"`, and hovering over a linked badge shows a tooltip listing the linked courses (`code - name`).
  - `DELETE /api/admin/users/{userId}` deletes a single user if they are not an admin, not the current user, and have no course links.
  - `POST /api/admin/users/delete-batch` deletes a selected list of eligible users (validating every user via `UserDeletionPolicy` before deleting) and returns `{ deletedCount }`.
  - In the UI, admins can check individual eligible users or toggle "Select all" for visible eligible users on the current page and execute "Delete Selected", with confirmation warnings (including an **extra prominent warning** whenever any teacher (`lector`) account is about to be deleted) and toast notifications (`ToastrService`) for operation feedback.
  - Bulk "Remove all orphaned users" at once is not present.

### AC-5: User Impersonation (Admin & Lector)
- **Given** an authenticated user with the `admin` or `lector` role (who is not currently impersonating another user)
- **When** they search for an eligible target user and click "Impersonate" (`POST /api/impersonation/{userId}`)
  - An `admin` can impersonate any non-admin user (`lector` or `student`).
  - A `lector` (who is not an `admin`) can only impersonate `student` users (not `lector` or `admin` users).
- **Then**:
  - The API issues a `TokenAccessPass` for the target user containing the target user's identity/role claims plus an `impersonator_id` claim with the caller's user ID.
  - The Angular `AuthService` stores the original `TokenModel` in `localStorage`, activates the impersonation token, invalidates/reloads the user profile and sidebar/courses, and navigates to `/`.
  - A persistent top banner is displayed throughout the app showing the impersonated user's email/roles, a **Read-Only Mode** indicator, and a **"Stop Impersonating"** button.
  - While impersonating, the UI is **read-only** (state-changing actions are disabled/blocked) and the backend rejects any state-changing HTTP requests (`POST`, `PUT`, `PATCH`, `DELETE`) with `403 Forbidden`.
  - Clicking **"Stop Impersonating"** restores the original `TokenModel`, invalidates/reloads the user profile and sidebar/courses, removes the banner, and returns the caller to their own session without re-logging in.

### AC-6: Angular Navigation & Guards
- **Given** an authenticated user in the Angular SPA
- **When** `userProfile.isAdmin()` is `true` (and not currently impersonating a non-admin)
- **Then** the sidebar displays an **Admin** collapsible section with links to **Periods**, **Courses**, and **Users & Impersonation**, protected by an `AdminGuard`.
- **When** `userProfile.isLector()` is `true` (and not `isAdmin()`, and not currently impersonating)
- **Then** the sidebar displays an **Impersonate Student** link (protected by a `LectorOrAdminGuard`) where the lector can search and impersonate students (without access to period/course/user deletion).
- **When** a user without the required role navigates to `/admin/*` or `/impersonate`
- **Then** access is denied / redirected.

### AC-7: Admin Role Promotion & Demotion
- **Given** an authenticated user with the `admin` role
- **When** they manage admin roles in `/api/admin/users` and the Admin Users UI
- **Then**:
  - `POST /api/admin/users/{userId}/promote-admin` promotes a `lector` (teacher) user to `admin`.
  - Promoting a user who does not hold the `lector` role (e.g., a student-only user) is rejected (`400 Bad Request`).
  - `POST /api/admin/users/{userId}/demote-admin` removes the `admin` role from an admin user **only if** at least one other `admin` remains in the system.
  - Attempting to demote the last remaining `admin` is rejected (`400 Bad Request`).

---

## 4. Domain Rules & Invariants

- **DR-1 (Period Validation)**: `Period` requires non-empty `Description`, `From < Until`, and no date overlap with any other period.
- **DR-2 (Period Deletion Rule)**: A `Period` can only be deleted if it is not the current/active period (`!IsActive`, i.e., past or future), enforced via `Contracts.Require(...)` in `Period`.
- **DR-3 (Course Validation)**: `Course` requires non-empty `Code` (max 20 chars), non-empty `Name`, and `Code` must be unique across courses.
- **DR-4 (User Deletion Eligibility)**: A `User` can only be deleted if:
  1. The target user does **not** hold the `admin` role.
  2. The target user is **not** the currently authenticated admin.
  3. The target user has **zero** references in `TestRun`, `TestResult`, `SolutionFile`, `ProjectTeamUser`, `StudentCourseProfile`, and `ProjectTeamAssessment` / `PeerAssessment` (both as evaluator and subject).
- **DR-5 (Impersonation Restrictions)**:
  1. Only users with the `admin` or `lector` role can initiate impersonation.
  2. Users holding the `admin` role cannot be impersonated.
  3. A `lector` (without `admin` role) can only impersonate users who hold the `student` role and do **not** hold the `lector` or `admin` role.
  4. Nested impersonation is forbidden: a token that already contains an `impersonator_id` claim cannot initiate another impersonation.
  5. **Read-Only Impersonation**: While impersonating (`impersonator_id` claim present), no state-changing operations (`POST`, `PUT`, `PATCH`, `DELETE`) are permitted; impersonation is strictly for inspecting the application.
- **DR-6 (Admin Role Promotion & Demotion)**:
  1. Only an existing `admin` can promote or demote another user's `admin` role.
  2. Only users holding the `lector` (teacher) role can be promoted to `admin`.
  3. There must always be at least 1 `admin` in the system; demoting the last remaining `admin` is forbidden.

---

## 5. Authorization & Security Rules

- **AdminOnlyPolicy**: `RequireRole(Role.Constants.Admin)` — required on `POST/PUT/DELETE /api/periods`, `POST/PUT/DELETE /api/courses`, `GET /api/admin/users`, and `DELETE /api/admin/users/*`.
- **LectorsOnlyPolicy**: Remains `RequireRole(Role.Constants.Lector)` only. Admins who are not lectors do not automatically pass `LectorsOnlyPolicy` (they can impersonate a lector if needed).
- **LectorOrAdminPolicy**: `RequireRole(Role.Constants.Admin, Role.Constants.Lector)` — required on `GET /api/impersonation/users` and `POST /api/impersonation/{userId}` (with role-based target filtering/validation per **DR-5**).
- **Students**: Receive `403 Forbidden` on all `AdminOnlyPolicy`, `LectorsOnlyPolicy`, and `LectorOrAdminPolicy` endpoints.
- **Unauthenticated**: Receive `401 Unauthorized` on all protected endpoints.

---

## 6. Edge Cases & Error Handling

| Scenario | Expected Behavior / Error Type | HTTP Status |
| :--- | :--- | :--- |
| Delete current active period | Throws `ContractException` (`Contracts.Require`) | `400 Bad Request` |
| Create/Update period with overlapping dates or `from >= until` | Throws `ContractException` (`Contracts.Require`) | `400 Bad Request` |
| Create/Update course with duplicate `Code` or empty fields | Throws `ContractException` (`Contracts.Require`) | `400 Bad Request` |
| Period / Course / User not found | Throws `DataNotFoundException` | `404 Not Found` |
| Delete user who is linked to course data, is an admin, or is self | Throws `ContractException` | `400 Bad Request` |
| Impersonate an admin user or initiate nested impersonation | Returns BadRequest / Forbidden | `400 Bad Request` / `403 Forbidden` |
| State-changing request (`POST`/`PUT`/`PATCH`/`DELETE`) while impersonating | Rejected by `ImpersonationReadOnlyFilter` | `403 Forbidden` |
| Non-admin calls admin endpoints | Authorization failure | `403 Forbidden` |

---

## 7. Out of Scope

- A UI for promoting/demoting users to/from the `admin` role (initial admins are configured via `appsettings` / DB).
- Server-side session/revocation store for impersonation tokens (impersonation uses standard short-lived JWTs with `impersonator_id` claim).
- Impersonating multiple levels deep (nested impersonation).

---

## 8. Open Questions

_All questions resolved via `/grill-me` interview._
