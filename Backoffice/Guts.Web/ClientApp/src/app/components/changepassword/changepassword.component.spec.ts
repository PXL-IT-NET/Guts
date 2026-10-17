import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { AuthService } from "../../services/auth.service";
import { PostResult } from "../../util/result";
import { ChangePasswordComponent } from "./changepassword.component";

describe("ChangePasswordComponent", () => {
  let component: ChangePasswordComponent;
  let fixture: ComponentFixture<ChangePasswordComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "changePassword",
    ]);
    TestBed.overrideComponent(ChangePasswordComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [ChangePasswordComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [{ provide: AuthService, useValue: authServiceSpy }],
    });
    fixture = TestBed.createComponent(ChangePasswordComponent);
    component = fixture.componentInstance;
  });

  it("changePassword should set error when passwords do not match", () => {
    component.model.newPassword = "Password1!";
    component.model.repeatNewPassword = "Password2!";

    component.changePassword();

    expect(component.error).toBe(
      "Please make sure all fields are filled in correctly.",
    );
    expect(authServiceSpy.changePassword).not.toHaveBeenCalled();
  });

  it("changePassword should set success=true and reset model when service succeeds", () => {
    authServiceSpy.changePassword.and.returnValue(of(PostResult.success()));
    component.model.currentPassword = "Old!";
    component.model.newPassword = "New!";
    component.model.repeatNewPassword = "New!";

    component.changePassword();

    expect(component.success).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.model.newPassword).toBe("");
  });

  it("changePassword should set error message when service fails", () => {
    authServiceSpy.changePassword.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Invalid current" }),
    );
    component.model.newPassword = "New!";
    component.model.repeatNewPassword = "New!";

    component.changePassword();

    expect(component.success).toBeFalse();
    expect(component.error).toBe("Invalid current");
  });
});

