import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { AuthService } from "../../services/auth.service";
import { PostResult } from "../../util/result";
import { ForgotPasswordComponent } from "./forgotpassword.component";

describe("ForgotPasswordComponent", () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "sendForgotPasswordMail",
    ]);
    TestBed.overrideComponent(ForgotPasswordComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [ForgotPasswordComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [{ provide: AuthService, useValue: authServiceSpy }],
    });
    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
  });

  it("sendMail should require captchaToken before calling service", () => {
    component.sendMail();
    expect(component.error).toBe("Please proof that you are not a robot");
    expect(authServiceSpy.sendForgotPasswordMail).not.toHaveBeenCalled();
  });

  it("sendMail should set success=true when service succeeds and reset captcha on error", () => {
    component.resolved("captcha-tok");
    authServiceSpy.sendForgotPasswordMail.and.returnValue(
      of(PostResult.success()),
    );

    component.sendMail();
    expect(component.success).toBeTrue();

    const resetSpy = jasmine.createSpy("reset");
    component.captcha = { reset: resetSpy } as any;
    authServiceSpy.sendForgotPasswordMail.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Mail error" }),
    );

    component.sendMail();
    expect(component.error).toBe("Mail error");
    expect(resetSpy).toHaveBeenCalled();
  });
});

