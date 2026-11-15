import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { of } from "rxjs";
import { AuthService } from "../../services/auth.service";
import { PostResult } from "../../util/result";
import { LectorVerificationSentComponent } from "./lector-verification-sent.component";

describe("LectorVerificationSentComponent", () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "resendLectorVerification",
    ]);
  });

  function createComponentWithHistoryState(
    state: Record<string, unknown>,
  ): ComponentFixture<LectorVerificationSentComponent> {
    globalThis.history.replaceState(state, "");
    TestBed.configureTestingModule({
      imports: [LectorVerificationSentComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    });
    const fixture = TestBed.createComponent(LectorVerificationSentComponent);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => {
    globalThis.history.replaceState({}, "");
  });

  it("should show email and allow resending verification email when state is provided", () => {
    authServiceSpy.resendLectorVerification.and.returnValue(
      of(PostResult.success()),
    );
    const fixture = createComponentWithHistoryState({
      email: "teacher@pxl.be",
      userId: 42,
      resendToken: "resend-tok",
    });

    const el = fixture.nativeElement as HTMLElement;
    expect(
      el.querySelector("[data-testid='verification-email-target']")?.textContent,
    ).toContain("teacher@pxl.be");

    const resendBtn = el.querySelector(
      "[data-testid='resend-verification-btn']",
    ) as HTMLButtonElement;
    expect(resendBtn).toBeTruthy();

    resendBtn.click();
    fixture.detectChanges();

    expect(authServiceSpy.resendLectorVerification).toHaveBeenCalledWith({
      userId: "42",
      resendToken: "resend-tok",
    });
    expect(
      el.querySelector("[data-testid='resend-success-alert']"),
    ).toBeTruthy();
  });

  it("should show error alert when resend fails and hide resend button when state is missing", () => {
    authServiceSpy.resendLectorVerification.and.returnValue(
      of({
        success: false,
        isAuthenticated: true,
        message: "Resend token expired.",
      }),
    );
    const fixture = createComponentWithHistoryState({
      email: "teacher@pxl.be",
      userId: 42,
      resendToken: "expired-tok",
    });

    fixture.componentInstance.resendVerificationEmail();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector("[data-testid='resend-error-alert']")
        ?.textContent,
    ).toContain("Resend token expired.");

    TestBed.resetTestingModule();
    const emptyFixture = createComponentWithHistoryState({});
    expect(
      emptyFixture.nativeElement.querySelector(
        "[data-testid='resend-verification-btn']",
      ),
    ).toBeNull();
  });
});
