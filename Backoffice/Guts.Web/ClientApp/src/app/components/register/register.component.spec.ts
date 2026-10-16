import { RegisterComponent } from "./register.component";
import { AuthService } from "../../services/auth.service";
import { PostResult } from "../../util/result";
import { of } from "rxjs";
import {
  createChangeDetectorRefSpy,
  createRouterSpy,
} from "../../testing/test-helpers";

describe("RegisterComponent", () => {
  let component: RegisterComponent;
  let routerSpy: ReturnType<typeof createRouterSpy>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let cdrSpy: ReturnType<typeof createChangeDetectorRefSpy>;

  beforeEach(() => {
    routerSpy = createRouterSpy();
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "register",
    ]);
    cdrSpy = createChangeDetectorRefSpy();

    component = new RegisterComponent(routerSpy, authServiceSpy, cdrSpy);
  });

  it("should set captchaToken when resolved is called", () => {
    component.resolved("captcha-token-123");
    expect(component.model.captchaToken).toBe("captcha-token-123");
  });

  it("should set error and not call service when passwords do not match", () => {
    component.model.password = "pass1";
    component.model.repeatpassword = "pass2";
    component.register();

    expect(component.error).toBe(
      "Please make sure all fields are filled in correctly.",
    );
    expect(authServiceSpy.register).not.toHaveBeenCalled();
  });

  it("should set error and not call service when captchaToken is missing", () => {
    component.model.password = "pass1";
    component.model.repeatpassword = "pass1";
    component.model.captchaToken = "";
    component.register();

    expect(component.error).toBe("Please proof that you are not a robot");
    expect(authServiceSpy.register).not.toHaveBeenCalled();
  });

  it("should set registered=true when registration succeeds", () => {
    authServiceSpy.register.and.returnValue(of(PostResult.success()));
    component.model.password = "pass1";
    component.model.repeatpassword = "pass1";
    component.model.captchaToken = "valid-captcha";

    component.register();

    expect(authServiceSpy.register).toHaveBeenCalledWith(component.model);
    expect(component.registered).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(cdrSpy.detectChanges).toHaveBeenCalled();
  });

  it("should set error and reset captcha when registration fails", () => {
    const failResult = new PostResult();
    failResult.success = false;
    failResult.message = "Email already taken";
    authServiceSpy.register.and.returnValue(of(failResult));

    const captchaSpy = jasmine.createSpyObj("RecaptchaDirective", ["reset"]);
    component.captcha = captchaSpy;
    component.model.password = "pass1";
    component.model.repeatpassword = "pass1";
    component.model.captchaToken = "valid-captcha";

    component.register();

    expect(component.registered).toBeFalse();
    expect(component.error).toBe("Email already taken");
    expect(captchaSpy.reset).toHaveBeenCalled();
    expect(component.loading).toBeFalse();
  });
});

