import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { BehaviorSubject, of } from "rxjs";
import { AuthService } from "src/app/services/auth.service";
import { UserProfile } from "src/app/viewmodels/user.model";
import { HomeComponent } from "./home.component";

describe("HomeComponent", () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let loggedInState$: BehaviorSubject<boolean>;

  beforeEach(() => {
    loggedInState$ = new BehaviorSubject<boolean>(false);
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getLoggedInState",
      "getUserProfile",
    ]);
    authServiceSpy.getLoggedInState.and.returnValue(
      loggedInState$.asObservable(),
    );

    TestBed.overrideComponent(HomeComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [HomeComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [{ provide: AuthService, useValue: authServiceSpy }],
    });
    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it("ngOnInit should load userProfile when logged in and clear it when logged out", () => {
    const profile = new UserProfile({
      id: 7,
      email: "user@pxl.be",
      roles: ["student"],
      teams: [],
      isAuthenticated: true,
    });
    authServiceSpy.getUserProfile.and.returnValue(of(profile));

    component.ngOnInit();
    expect(component.userProfile.isAuthenticated).toBeFalse();

    loggedInState$.next(true);
    expect(component.userProfile).toBe(profile);

    loggedInState$.next(false);
    expect(component.userProfile.isAuthenticated).toBeFalse();
  });
});

