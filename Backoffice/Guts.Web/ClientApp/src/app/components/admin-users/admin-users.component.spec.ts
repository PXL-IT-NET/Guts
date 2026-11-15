import { ComponentFixture, TestBed } from "@angular/core/testing";
import { HttpErrorResponse } from "@angular/common/http";
import { of } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { AdminUsersComponent } from "./admin-users.component";
import { AdminUserService, IAdminUser } from "../../services/admin-user.service";
import { ImpersonationService } from "../../services/impersonation.service";
import { AuthService } from "../../services/auth.service";
import { GetResult, PostResult } from "../../util/result";
import { createToastrSpy } from "../../testing/test-helpers";

describe("AdminUsersComponent", () => {
  let component: AdminUsersComponent;
  let fixture: ComponentFixture<AdminUsersComponent>;
  let adminUserServiceSpy: jasmine.SpyObj<AdminUserService>;
  let impersonationServiceSpy: jasmine.SpyObj<ImpersonationService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  const lectorUser: IAdminUser = {
    id: 10,
    email: "teacher@pxl.be",
    firstName: "Jane",
    lastName: "Doe",
    roles: ["lector"],
    isLinkedToCourse: false,
    linkedCourses: [],
    canDelete: true,
    canImpersonate: true,
    canPromoteToAdmin: true,
    canDemoteFromAdmin: false,
    lastVerifiedOnUtc: null,
    isLockedLector: true,
    canUnlockLector: true,
  };

  const student1: IAdminUser = {
    id: 20,
    email: "student1@student.pxl.be",
    firstName: "Alice",
    lastName: "Student",
    roles: ["student"],
    isLinkedToCourse: false,
    linkedCourses: [],
    canDelete: true,
    canImpersonate: true,
    canPromoteToAdmin: false,
    canDemoteFromAdmin: false,
    lastVerifiedOnUtc: null,
    isLockedLector: false,
    canUnlockLector: false,
  };

  const linkedStudent: IAdminUser = {
    id: 21,
    email: "linked@student.pxl.be",
    firstName: "Bob",
    lastName: "Linked",
    roles: ["student"],
    isLinkedToCourse: true,
    linkedCourses: [
      { id: 1, code: "DOTNET1", name: ".NET Essentials" },
      { id: 2, code: "JAVA1", name: "Java Programming" },
    ],
    canDelete: true,
    canImpersonate: true,
    canPromoteToAdmin: false,
    canDemoteFromAdmin: false,
    lastVerifiedOnUtc: null,
    isLockedLector: false,
    canUnlockLector: false,
  };

  beforeEach(async () => {
    adminUserServiceSpy = jasmine.createSpyObj("AdminUserService", [
      "searchUsers",
      "deleteUser",
      "deleteUsers",
      "promoteToAdmin",
      "demoteFromAdmin",
      "unlockLector",
    ]);
    impersonationServiceSpy = jasmine.createSpyObj("ImpersonationService", [
      "impersonate",
    ]);
    authServiceSpy = jasmine.createSpyObj("AuthService", ["startImpersonation"]);

    adminUserServiceSpy.searchUsers.and.returnValue(
      of(
        GetResult.success({
          items: [lectorUser, student1, linkedStudent],
          totalCount: 3,
          page: 1,
          pageSize: 25,
        }),
      ),
    );
    adminUserServiceSpy.deleteUser.and.returnValue(of(PostResult.success()));
    adminUserServiceSpy.deleteUsers.and.returnValue(
      of(GetResult.success({ deletedCount: 2 })),
    );
    adminUserServiceSpy.unlockLector.and.returnValue(of(PostResult.success()));
    toastrSpy = createToastrSpy();

    await TestBed.configureTestingModule({
      imports: [AdminUsersComponent],
      providers: [
        { provide: AdminUserService, useValue: adminUserServiceSpy },
        { provide: ImpersonationService, useValue: impersonationServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUsersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should show extra teacher warning when requesting deletion of a lector", () => {
    component.requestDelete(lectorUser);
    fixture.detectChanges();

    const warning = fixture.nativeElement.querySelector(
      "[data-testid='teacher-delete-warning']",
    );
    expect(warning).toBeTruthy();
    expect(warning.textContent).toContain("TEACHER");
  });

  it("should show extra linked data warning and require confirmation when requesting deletion of a linked student", () => {
    component.requestDelete(linkedStudent);
    fixture.detectChanges();

    const warning = fixture.nativeElement.querySelector(
      "[data-testid='linked-delete-warning']",
    );
    expect(warning).toBeTruthy();
    expect(warning.textContent).toContain("linked to course data");
    expect(component.requiresSingleDeleteConfirmation(linkedStudent)).toBeTrue();
  });

  it("should allow selecting all visible deletable users including linked users and deleting them with toast confirmation", () => {
    const selectAllCheckbox = fixture.nativeElement.querySelector(
      "[data-testid='select-all-users-checkbox']",
    ) as HTMLInputElement;
    selectAllCheckbox.click();
    fixture.detectChanges();

    expect(component.selectedCount()).toBe(3);
    expect(component.isSelected(linkedStudent)).toBeTrue();

    const deleteSelectedBtn = fixture.nativeElement.querySelector(
      "[data-testid='delete-selected-users-btn']",
    ) as HTMLButtonElement;
    deleteSelectedBtn.click();
    fixture.detectChanges();

    const confirmBtn = fixture.nativeElement.querySelector(
      "[data-testid='confirm-batch-delete-btn']",
    ) as HTMLButtonElement;
    confirmBtn.click();
    fixture.detectChanges();

    expect(adminUserServiceSpy.deleteUsers).toHaveBeenCalledWith([10, 20, 21]);
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Deleted 2 user(s).",
      "Success",
    );
  });

  it("should change page size including All and trigger search", () => {
    component.onPageSizeChange(100);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "",
      false,
      1,
      100,
      false,
    );

    component.onPageSizeChange(null);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "",
      false,
      1,
      null,
      false,
    );
  });

  it("should navigate pages via goToPage", () => {
    adminUserServiceSpy.searchUsers.and.returnValue(
      of(
        GetResult.success({
          items: [student1],
          totalCount: 60,
          page: 1,
          pageSize: 25,
        }),
      ),
    );
    component.loadUsers();
    fixture.detectChanges();

    expect(component.totalPages()).toBe(3);

    component.goToPage(2);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "",
      false,
      2,
      25,
      false,
    );
  });

  it("should show toastr error when deleteUsers fails", () => {
    adminUserServiceSpy.deleteUsers.and.returnValue(
      of(
        GetResult.fromHttpErrorResponse<{ deletedCount: number }>(
          new HttpErrorResponse({
            status: 400,
            error: "Cannot delete linked user",
          }),
        ),
      ),
    );

    component.toggleUserSelection(student1, true);
    component.requestDeleteSelected();
    component.confirmBatchDelete();

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Cannot delete linked user",
      "Error",
    );
  });

  it("should display Orphaned or Linked to x courses with hover tooltip listing courses", () => {
    const badges = fixture.nativeElement.querySelectorAll(
      "[data-testid='course-links-badge']",
    ) as NodeListOf<HTMLElement>;
    expect(badges).toHaveSize(3);
    expect(badges[0].textContent?.trim()).toBe("Orphaned");
    expect(badges[2].textContent?.trim()).toBe("Linked to 2 courses");
    expect(badges[2].getAttribute("title")).toBe(
      "DOTNET1 - .NET Essentials\nJAVA1 - Java Programming",
    );
  });

  it("should filter immediately when selecting a role or toggling orphaned/locked only and reset filters", () => {
    component.selectRoleFilter("student");
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "student",
      false,
      1,
      25,
      false,
    );

    component.onOrphanedOnlyChange(true);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "student",
      true,
      1,
      25,
      false,
    );

    component.onLockedOnlyChange(true);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "student",
      true,
      1,
      25,
      true,
    );

    component.searchTerm = "alice";
    component.resetFilters();
    expect(component.searchTerm).toBe("");
    expect(component.roleFilter).toBe("");
    expect(component.orphanedOnly).toBeFalse();
    expect(component.lockedOnly).toBeFalse();
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "",
      false,
      1,
      25,
      false,
    );
  });

  it("should display locked verification badge for locked lector and require confirmation modal before unlocking", () => {
    const verificationBadges = fixture.nativeElement.querySelectorAll(
      "[data-testid='lector-verification-badge']",
    ) as NodeListOf<HTMLElement>;
    expect(verificationBadges).toHaveSize(1);
    expect(verificationBadges[0].textContent?.trim()).toBe("Locked");

    const unlockBtn = fixture.nativeElement.querySelector(
      "[data-testid='unlock-lector-btn-10']",
    ) as HTMLButtonElement;
    unlockBtn.click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector("[data-testid='unlock-lector-modal']"),
    ).toBeTruthy();

    const cancelBtn = fixture.nativeElement.querySelector(
      "[data-testid='cancel-unlock-lector-btn']",
    ) as HTMLButtonElement;
    cancelBtn.click();
    fixture.detectChanges();
    expect(adminUserServiceSpy.unlockLector).not.toHaveBeenCalled();

    unlockBtn.click();
    fixture.detectChanges();
    const confirmBtn = fixture.nativeElement.querySelector(
      "[data-testid='confirm-unlock-lector-btn']",
    ) as HTMLButtonElement;
    confirmBtn.click();
    fixture.detectChanges();

    expect(adminUserServiceSpy.unlockLector).toHaveBeenCalledWith(10);
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Lector 'teacher@pxl.be' unlocked.",
      "Success",
    );
  });

  it("should render centered pagination and items-per-page selector in the pagination footer", () => {
    const footer = fixture.nativeElement.querySelector(".pagination-footer");
    expect(footer).toBeTruthy();
    expect(
      footer.querySelector("[data-testid='users-pagination'] ul.justify-content-center"),
    ).toBeTruthy();
    expect(
      footer.querySelector("[data-testid='page-size-select']"),
    ).toBeTruthy();
  });

  it("should handle search/filter helpers, page bounds clamping, and load error", () => {
    adminUserServiceSpy.searchUsers.calls.reset();
    component.selectRoleFilter("");
    expect(adminUserServiceSpy.searchUsers).not.toHaveBeenCalled();

    component.searchTerm = "abc";
    component.clearSearchTerm();
    expect(component.searchTerm).toBe("");

    component.goToPage(0);
    component.goToPage(1);
    component.goToPage(99);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledTimes(1);

    adminUserServiceSpy.searchUsers.and.returnValues(
      of(
        GetResult.success({
          items: [],
          totalCount: 5,
          page: 2,
          pageSize: 2,
        }),
      ),
      of(
        GetResult.success({
          items: [student1],
          totalCount: 5,
          page: 3,
          pageSize: 2,
        }),
      ),
    );
    component.pageSize.set(2);
    component.currentPage.set(2);
    component.loadUsers();
    expect(component.totalCount()).toBe(5);

    adminUserServiceSpy.searchUsers.and.returnValue(
      of(
        GetResult.fromHttpErrorResponse<never>(
          new HttpErrorResponse({ status: 400, error: "Server fail" }),
        ),
      ),
    );
    component.loadUsers();
    expect(toastrSpy.error).toHaveBeenCalledWith("Server fail", "Error");
  });

  it("should format role badges, verification tooltips, course links, selection, and delete warnings", () => {
    expect(component.getRoleBadgeClass("admin")).toBe("text-bg-dark");
    expect(component.getRoleBadgeClass("lector")).toBe("text-bg-warning");
    expect(component.getRoleBadgeClass("student")).toBe("text-bg-secondary");

    expect(
      component.getVerificationTooltip({
        ...lectorUser,
        lastVerifiedOnUtc: "2026-01-15T10:00:00Z",
      }),
    ).toBe("Last verified: 2026-01-15 (UTC)");

    expect(component.getCourseLinksLabel(student1)).toBe("Linked");
    expect(component.getLinkedCoursesTooltip(student1)).toBeNull();
    expect(
      component.getCourseLinksLabel({
        ...linkedStudent,
        linkedCourses: [{ id: 1, code: "C1", name: "Course 1" }],
      }),
    ).toBe("Linked to 1 course");

    const lockedStudent: IAdminUser = { ...student1, canDelete: false };
    component.toggleUserSelection(lockedStudent, true);
    expect(component.isSelected(lockedStudent)).toBeFalse();
    component.toggleUserSelection(student1, true);
    component.toggleUserSelection(student1, false);
    expect(component.isSelected(student1)).toBeFalse();
    component.clearSelection();
    expect(component.selectedCount()).toBe(0);

    expect(component.getSingleDeleteExtraWarning(null)).toBeNull();
    expect(
      component.getSingleDeleteExtraWarning({
        ...lectorUser,
        isLinkedToCourse: true,
      }),
    ).toContain("TEACHER (lector) linked to course data");
    expect(component.getSingleDeleteExtraWarning(student1)).toBeNull();

    component.toggleUserSelection(lectorUser, true);
    expect(component.getBatchDeleteExtraWarning()).toContain(
      "TEACHER (lector) accounts",
    );
    component.clearSelection();
    component.toggleUserSelection(linkedStudent, true);
    expect(component.getBatchDeleteExtraWarning()).toContain(
      "linked to course data",
    );
  });

  it("should handle impersonate, delete, unlock, promote, and demote success and failure paths", () => {
    impersonationServiceSpy.impersonate.and.returnValue(
      of(GetResult.success({ token: "imp", expiration: "2026-12-01" })),
    );
    component.impersonate(student1);
    expect(authServiceSpy.startImpersonation).toHaveBeenCalled();

    impersonationServiceSpy.impersonate.and.returnValue(
      of(
        GetResult.fromHttpErrorResponse<never>(
          new HttpErrorResponse({ status: 403, error: "Forbidden" }),
        ),
      ),
    );
    component.impersonate(student1);
    expect(toastrSpy.error).toHaveBeenCalledWith("Forbidden", "Error");

    component.requestDelete(student1);
    component.cancelDelete();
    component.confirmDelete();
    expect(adminUserServiceSpy.deleteUser).not.toHaveBeenCalled();

    adminUserServiceSpy.deleteUser.and.returnValues(
      of(PostResult.success()),
      of(
        PostResult.fromHttpErrorResponse(
          new HttpErrorResponse({ status: 400, error: "Cannot delete" }),
        ),
      ),
    );
    component.requestDelete(student1);
    component.confirmDelete();
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "User 'student1@student.pxl.be' deleted.",
      "Success",
    );
    component.requestDelete(student1);
    component.confirmDelete();
    expect(toastrSpy.error).toHaveBeenCalledWith("Cannot delete", "Error");

    component.clearSelection();
    component.requestDeleteSelected();
    expect(toastrSpy.warning).toHaveBeenCalled();
    component.cancelBatchDelete();
    component.confirmBatchDelete();

    adminUserServiceSpy.unlockLector.and.returnValue(
      of(
        PostResult.fromHttpErrorResponse(
          new HttpErrorResponse({ status: 400, error: "Unlock failed" }),
        ),
      ),
    );
    component.confirmUnlock();
    component.requestUnlock(lectorUser);
    component.confirmUnlock();
    expect(toastrSpy.error).toHaveBeenCalledWith("Unlock failed", "Error");

    adminUserServiceSpy.promoteToAdmin.and.returnValues(
      of(PostResult.success()),
      of(
        PostResult.fromHttpErrorResponse(
          new HttpErrorResponse({ status: 400, error: "Promote fail" }),
        ),
      ),
    );
    component.promoteToAdmin(lectorUser);
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "'teacher@pxl.be' is now an administrator.",
      "Success",
    );
    component.promoteToAdmin(lectorUser);
    expect(toastrSpy.error).toHaveBeenCalledWith("Promote fail", "Error");

    adminUserServiceSpy.demoteFromAdmin.and.returnValues(
      of(PostResult.success()),
      of(
        PostResult.fromHttpErrorResponse(
          new HttpErrorResponse({ status: 400, error: "Demote fail" }),
        ),
      ),
    );
    component.demoteFromAdmin(lectorUser);
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Removed administrator role from 'teacher@pxl.be'.",
      "Success",
    );
    component.demoteFromAdmin(lectorUser);
    expect(toastrSpy.error).toHaveBeenCalledWith("Demote fail", "Error");
  });
});
