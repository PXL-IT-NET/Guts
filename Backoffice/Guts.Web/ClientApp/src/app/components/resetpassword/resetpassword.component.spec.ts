import { ResetPasswordComponent } from "./resetpassword.component";
import { AuthService } from "../../services/auth.service";
import { PostResult } from "../../util/result";
import { of } from "rxjs";
import {
  ActivatedRouteStub,
  createChangeDetectorRefSpy,
} from "../../testing/test-helpers";

describe("ResetPasswordComponent", () => {
  let component: ResetPasswordComponent;
  let routeStub: ActivatedRouteStub;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let cdrSpy: ReturnType<typeof createChangeDetectorRefSpy>;

  beforeEach(() => {
    routeStub = new ActivatedRouteStub(
      {},
      { userId: "user-42", token: "tok-99" },
    );
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "resetPassword",
    ]);
    cdrSpy = createChangeDetectorRefSpy();

    component = new ResetPasswordComponent(
      routeStub as any,
      authServiceSpy,
      cdrSpy,
    );
  });

  it("should read userId and token from queryParams on ngOnInit", () => {
    component.ngOnInit();
    expect(component.model.userId).toBe("user-42");
    expect(component.model.token).toBe("tok-99");
  });

  it("should set error and not call service when passwords do not match", () => {
    component.model.password = "newPass1";
    component.model.repeatPassword = "newPass2";

    component.resetPassword();

    expect(component.error).toBe(
      "Please make sure all fields are filled in correctly.",
    );
    expect(authServiceSpy.resetPassword).not.toHaveBeenCalled();
  });

  it("should set success=true when resetPassword succeeds", () => {
    authServiceSpy.resetPassword.and.returnValue(of(PostResult.success()));
    component.ngOnInit();
    component.model.password = "newPass1";
    component.model.repeatPassword = "newPass1";

    component.resetPassword();

    expect(authServiceSpy.resetPassword).toHaveBeenCalledWith(component.model);
    expect(component.success).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(cdrSpy.detectChanges).toHaveBeenCalled();
  });

  it("should set error message when resetPassword fails", () => {
    const failResult = new PostResult();
    failResult.success = false;
    failResult.message = "Invalid token";
    authServiceSpy.resetPassword.and.returnValue(of(failResult));

    component.model.password = "newPass1";
    component.model.repeatPassword = "newPass1";

    component.resetPassword();

    expect(component.success).toBeFalse();
    expect(component.error).toBe("Invalid token");
    expect(component.loading).toBeFalse();
  });
});

