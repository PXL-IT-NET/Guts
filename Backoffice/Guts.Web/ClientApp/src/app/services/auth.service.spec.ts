import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { LocalStorageKeys } from "../util/localstorage.keys";
import { ChangePasswordModel } from "../viewmodels/changepassword.model";
import { ConfirmEmailModel } from "../viewmodels/confirmemail.model";
import { ForgotPasswordModel } from "../viewmodels/forgotpassword.model";
import { LoginModel } from "../viewmodels/login.model";
import { RegisterModel } from "../viewmodels/register.model";
import { ResetPasswordModel } from "../viewmodels/resetpassword.model";
import { AuthService } from "./auth.service";

describe("AuthService", () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.removeItem(LocalStorageKeys.currentToken);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.removeItem(LocalStorageKeys.currentToken);
  });

  it("should initialize loggedInState from localStorage token when present", () => {
    localStorage.setItem(
      LocalStorageKeys.currentToken,
      JSON.stringify({ token: "saved-jwt" }),
    );
    const freshService = new AuthService(TestBed.inject(AuthService)["http"]);

    let isLoggedIn = false;
    freshService.getLoggedInState().subscribe((state) => (isLoggedIn = state));

    expect(isLoggedIn).toBeTrue();
    expect(freshService.getToken()).toBe("saved-jwt");
  });

  it("login should store token, emit loggedInState=true, and return success", () => {
    const model = new LoginModel();
    model.email = "student@pxl.be";
    model.password = "Secret123!";

    let resultSuccess = false;
    let isLoggedIn = false;
    service.getLoggedInState().subscribe((state) => (isLoggedIn = state));

    service.login(model).subscribe((result) => {
      resultSuccess = result.success;
    });

    const req = httpMock.expectOne("api/auth/token");
    expect(req.request.method).toBe("POST");
    req.flush({ token: "jwt-token" });

    expect(resultSuccess).toBeTrue();
    expect(isLoggedIn).toBeTrue();
    expect(service.getToken()).toBe("jwt-token");
  });

  it("login should return unauthenticated result when response has no token", () => {
    const model = new LoginModel();
    let actualResult: any;

    service.login(model).subscribe((result) => (actualResult = result));

    httpMock.expectOne("api/auth/token").flush({});

    expect(actualResult.success).toBeFalse();
    expect(actualResult.isAuthenticated).toBeFalse();
  });

  it("login should handle HTTP error response", () => {
    const model = new LoginModel();
    let actualResult: any;

    service.login(model).subscribe((result) => (actualResult = result));

    httpMock
      .expectOne("api/auth/token")
      .flush("Unauthorized", { status: 401, statusText: "Unauthorized" });

    expect(actualResult.success).toBeFalse();
    expect(actualResult.isAuthenticated).toBeFalse();
  });

  it("clearToken and logout should remove token and emit loggedInState=false", () => {
    localStorage.setItem(
      LocalStorageKeys.currentToken,
      JSON.stringify({ token: "jwt" }),
    );
    service.clearToken();
    expect(service.getToken()).toBeNull();

    service.logout();
    expect(localStorage.getItem(LocalStorageKeys.currentToken)).toBeNull();
  });

  it("cancelLoginSession should PATCH cancel endpoint and return success", () => {
    let success = false;
    service
      .cancelLoginSession("session-123")
      .subscribe((res) => (success = res.success));

    const req = httpMock.expectOne("api/auth/loginsession/session-123/cancel");
    expect(req.request.method).toBe("PATCH");
    req.flush({});

    expect(success).toBeTrue();
  });

  it("getUserProfile should fetch and cache profile until invalidated", () => {
    const rawProfile = {
      id: 1,
      email: "user@pxl.be",
      roles: ["student"],
      teams: [2],
      isAuthenticated: true,
    };

    let fetchedId = 0;
    service.getUserProfile().subscribe((p) => (fetchedId = p.id));

    const req = httpMock.expectOne("api/users/current/profile");
    req.flush(rawProfile);
    expect(fetchedId).toBe(1);

    // Second call uses cached profile without HTTP request
    service.getUserProfile().subscribe((p) => (fetchedId = p.id));
    httpMock.expectNone("api/users/current/profile");

    // After invalidating cache, it fetches again
    service.invalidateUserProfileCache();
    service.getUserProfile().subscribe();
    httpMock.expectOne("api/users/current/profile").flush(rawProfile);
  });

  it("register, confirmEmail, sendForgotPasswordMail, and resetPassword should handle success and error", () => {
    const endpoints: Array<{
      invoke: () => any;
      url: string;
    }> = [
      {
        invoke: () => service.register(new RegisterModel()),
        url: "api/auth/register",
      },
      {
        invoke: () => service.confirmEmail(new ConfirmEmailModel()),
        url: "api/auth/confirmemail",
      },
      {
        invoke: () => service.sendForgotPasswordMail(new ForgotPasswordModel()),
        url: "api/auth/forgotpassword",
      },
      {
        invoke: () => service.resetPassword(new ResetPasswordModel()),
        url: "api/auth/resetpassword",
      },
    ];

    for (const entry of endpoints) {
      let ok = false;
      entry.invoke().subscribe((res: any) => (ok = res.success));
      const req = httpMock.expectOne(entry.url);
      expect(req.request.method).toBe("POST");
      req.flush({});
      expect(ok).toBeTrue();

      let failed = true;
      entry.invoke().subscribe((res: any) => (failed = res.success));
      httpMock
        .expectOne(entry.url)
        .flush(
          { error: "Failed" },
          { status: 400, statusText: "Bad Request" },
        );
      expect(failed).toBeFalse();
    }
  });

  it("changePassword should post current and new password and return success", () => {
    const model = new ChangePasswordModel();
    model.currentPassword = "old-password";
    model.newPassword = "new-password";
    model.repeatNewPassword = "new-password";

    let actualSuccess = false;

    service.changePassword(model).subscribe((result) => {
      actualSuccess = result.success;
    });

    const request = httpMock.expectOne("api/auth/changepassword");
    expect(request.request.method).toBe("POST");
    expect(request.request.body).toEqual({
      currentPassword: model.currentPassword,
      newPassword: model.newPassword,
    });

    request.flush({});

    expect(actualSuccess).toBeTrue();
  });

  it("changePassword should return failed result when api returns bad request", () => {
    const model = new ChangePasswordModel();
    model.currentPassword = "wrong-current-password";
    model.newPassword = "new-password";

    let actualSuccess = true;

    service.changePassword(model).subscribe((result) => {
      actualSuccess = result.success;
      expect(result.message).toContain("Current password is incorrect");
    });

    const request = httpMock.expectOne("api/auth/changepassword");
    request.flush(
      { CurrentPassword: ["Current password is incorrect"] },
      { status: 400, statusText: "Bad Request" },
    );

    expect(actualSuccess).toBeFalse();
  });

  it("isLoggedIn signal should reflect logged in state across login and logout", () => {
    expect(service.isLoggedIn()).toBeFalse();

    const model = new LoginModel();
    model.email = "student@pxl.be";
    model.password = "Secret123!";

    service.login(model).subscribe();
    httpMock.expectOne("api/auth/token").flush({ token: "new-jwt" });

    expect(service.isLoggedIn()).toBeTrue();

    service.logout();
    expect(service.isLoggedIn()).toBeFalse();
  });
});
