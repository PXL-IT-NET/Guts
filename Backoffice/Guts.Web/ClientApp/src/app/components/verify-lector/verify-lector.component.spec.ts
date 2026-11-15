import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { of } from "rxjs";
import { AuthService } from "../../services/auth.service";
import { createActivatedRouteProvider } from "../../testing/test-helpers";
import { PostResult } from "../../util/result";
import { VerifyLectorComponent } from "./verify-lector.component";

describe("VerifyLectorComponent", () => {
  let component: VerifyLectorComponent;
  let fixture: ComponentFixture<VerifyLectorComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "verifyLector",
    ]);

    TestBed.configureTestingModule({
      imports: [VerifyLectorComponent],
      providers: [
        provideRouter([]),
        createActivatedRouteProvider({}, {}, { userId: "42", token: "tok123" }),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    });
    fixture = TestBed.createComponent(VerifyLectorComponent);
    component = fixture.componentInstance;
  });

  it("should verify lector on init and show unlocked message with Log in link", () => {
    authServiceSpy.verifyLector.and.returnValue(of(PostResult.success()));

    fixture.detectChanges();

    expect(authServiceSpy.verifyLector).toHaveBeenCalledWith({
      userId: "42",
      token: "tok123",
    });
    expect(component.verified()).toBeTrue();
    expect(component.loading()).toBeFalse();
    expect(component.message()).toContain("unlocked");
    expect(
      fixture.nativeElement.querySelector(
        "[data-testid='verify-lector-login-link']",
      )?.textContent,
    ).toContain("Log in");
  });

  it("should show error message when verification fails", () => {
    authServiceSpy.verifyLector.and.returnValue(
      of({
        success: false,
        isAuthenticated: true,
        message: "Invalid or expired verification token.",
      }),
    );

    fixture.detectChanges();

    expect(component.verified()).toBeFalse();
    expect(component.loading()).toBeFalse();
    expect(component.message()).toContain("Invalid or expired");
    expect(
      fixture.nativeElement.querySelector(
        "[data-testid='verify-lector-login-link']",
      )?.textContent,
    ).toContain("Back to Login");
  });
});
