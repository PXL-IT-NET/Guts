import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { AuthService } from "../../services/auth.service";
import {
  ActivatedRouteStub,
  createActivatedRouteProvider,
} from "../../testing/test-helpers";
import { PostResult } from "../../util/result";
import { ConfirmEmailComponent } from "./confirmemail.component";

describe("ConfirmEmailComponent", () => {
  let component: ConfirmEmailComponent;
  let fixture: ComponentFixture<ConfirmEmailComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routeStub: ActivatedRouteStub;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "confirmEmail",
    ]);
    const routeProvider = createActivatedRouteProvider(
      {},
      {},
      { userId: "u1", token: "tok1" },
    );
    routeStub = routeProvider.useValue;

    TestBed.overrideComponent(ConfirmEmailComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [ConfirmEmailComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        routeProvider,
        { provide: AuthService, useValue: authServiceSpy },
      ],
    });
    fixture = TestBed.createComponent(ConfirmEmailComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it("ngOnInit should confirm email from queryParams and set success message", () => {
    authServiceSpy.confirmEmail.and.returnValue(of(PostResult.success()));

    component.ngOnInit();

    expect(authServiceSpy.confirmEmail).toHaveBeenCalledWith({
      userId: "u1",
      token: "tok1",
    });
    expect(component.confirmed).toBeTrue();
    expect(component.loading).toBeFalse();
    expect(component.message).toContain("succesfully confirmed");
  });

  it("ngOnInit should set error message when confirmEmail fails", () => {
    authServiceSpy.confirmEmail.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Expired token" }),
    );

    component.ngOnInit();

    expect(component.confirmed).toBeFalse();
    expect(component.message).toBe("Expired token");
  });
});

