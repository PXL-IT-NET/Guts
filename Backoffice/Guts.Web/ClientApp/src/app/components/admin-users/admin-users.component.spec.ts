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
    canDelete: false,
    canImpersonate: true,
    canPromoteToAdmin: false,
    canDemoteFromAdmin: false,
  };

  beforeEach(async () => {
    adminUserServiceSpy = jasmine.createSpyObj("AdminUserService", [
      "searchUsers",
      "deleteUser",
      "deleteUsers",
      "promoteToAdmin",
      "demoteFromAdmin",
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

  it("should allow selecting all visible deletable users and deleting them with toast confirmation", () => {
    const selectAllCheckbox = fixture.nativeElement.querySelector(
      "[data-testid='select-all-users-checkbox']",
    ) as HTMLInputElement;
    selectAllCheckbox.click();
    fixture.detectChanges();

    expect(component.selectedCount()).toBe(2);
    expect(component.isSelected(linkedStudent)).toBeFalse();

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

    expect(adminUserServiceSpy.deleteUsers).toHaveBeenCalledWith([10, 20]);
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
    );

    component.onPageSizeChange(null);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "",
      false,
      1,
      null,
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
    expect(badges.length).toBe(3);
    expect(badges[0].textContent?.trim()).toBe("Orphaned");
    expect(badges[2].textContent?.trim()).toBe("Linked to 2 courses");
    expect(badges[2].getAttribute("title")).toBe(
      "DOTNET1 - .NET Essentials\nJAVA1 - Java Programming",
    );
  });

  it("should filter immediately when selecting a role or toggling orphaned only and reset filters", () => {
    component.selectRoleFilter("student");
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "student",
      false,
      1,
      25,
    );

    component.onOrphanedOnlyChange(true);
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "student",
      true,
      1,
      25,
    );

    component.searchTerm = "alice";
    component.resetFilters();
    expect(component.searchTerm).toBe("");
    expect(component.roleFilter).toBe("");
    expect(component.orphanedOnly).toBeFalse();
    expect(adminUserServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      "",
      false,
      1,
      25,
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
});
