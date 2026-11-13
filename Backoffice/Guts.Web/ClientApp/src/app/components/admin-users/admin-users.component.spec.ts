import { ComponentFixture, TestBed } from "@angular/core/testing";
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
    canDelete: true,
    canImpersonate: true,
    canPromoteToAdmin: true,
    canDemoteFromAdmin: false,
  };

  beforeEach(async () => {
    adminUserServiceSpy = jasmine.createSpyObj("AdminUserService", [
      "searchUsers",
      "deleteUser",
      "deleteAllOrphanedUsers",
      "promoteToAdmin",
      "demoteFromAdmin",
    ]);
    impersonationServiceSpy = jasmine.createSpyObj("ImpersonationService", [
      "impersonate",
    ]);
    authServiceSpy = jasmine.createSpyObj("AuthService", ["startImpersonation"]);

    adminUserServiceSpy.searchUsers.and.returnValue(
      of(GetResult.success([lectorUser])),
    );
    adminUserServiceSpy.deleteUser.and.returnValue(of(PostResult.success()));
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
});
