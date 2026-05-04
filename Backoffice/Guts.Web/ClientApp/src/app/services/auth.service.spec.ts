import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { AuthService } from "./auth.service";
import { ChangePasswordModel } from "../viewmodels/changepassword.model";

describe("AuthService", () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
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
});
