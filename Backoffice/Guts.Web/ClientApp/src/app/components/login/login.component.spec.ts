import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";
import { of } from "rxjs";
import { AuthService } from "src/app/services";
import {
  createActivatedRouteProvider,
  createRouterSpy,
} from "src/app/testing/test-helpers";
import { PostResult } from "src/app/util/result";
import { LoginComponent } from "./login.component";

describe("LoginComponent", () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "logout",
      "login",
      "cancelLoginSession",
    ]);
    routerSpy = createRouterSpy();

    TestBed.overrideComponent(LoginComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [LoginComponent],
      imports: [FormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider({}, {}, { s: "session-abc" }),
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authServiceSpy },
      ],
    });
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("should capture loginSessionPublicIdentifier from queryParams and logout on init", () => {
    component.ngOnInit();
    expect(component.model.loginSessionPublicIdentifier).toBe("session-abc");
    expect(authServiceSpy.logout).toHaveBeenCalled();
  });

  it("login should navigate on success, set wrong credentials error when unauthenticated, or set server message", () => {
    authServiceSpy.login.and.returnValue(of(PostResult.success()));
    component.login();
    expect(routerSpy.navigate).toHaveBeenCalledWith(["/"]);

    authServiceSpy.login.and.returnValue(
      of({ success: false, isAuthenticated: false }),
    );
    component.login();
    expect(component.error).toBe("Wrong email-password combination");

    authServiceSpy.login.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Locked out" }),
    );
    component.login();
    expect(component.error).toBe("Locked out");
  });

  it("cancelSession should call cancelLoginSession and navigate to /", () => {
    authServiceSpy.cancelLoginSession.and.returnValue(of(PostResult.success()));

    component.cancelSession();

    expect(authServiceSpy.cancelLoginSession).toHaveBeenCalledWith(
      "session-abc",
    );
    expect(routerSpy.navigate).toHaveBeenCalledWith(["/"]);
  });
});
