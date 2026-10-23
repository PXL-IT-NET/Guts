import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import { BehaviorSubject, of } from "rxjs";
import { PeriodProvider } from "src/app/services/period.provider";
import { PeriodService } from "src/app/services/period.service";
import { GetResult } from "src/app/util/result";
import { PeriodModel } from "src/app/viewmodels/period.model";
import { AuthService } from "../../services/auth.service";
import { createRouterSpy } from "../../testing/test-helpers";
import { UserProfile } from "../../viewmodels/user.model";
import { NavMenuComponent } from "./nav-menu.component";

describe("NavMenuComponent", () => {
  let component: NavMenuComponent;
  let fixture: ComponentFixture<NavMenuComponent>;
  let routerSpy: jasmine.SpyObj<Router>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let periodServiceSpy: jasmine.SpyObj<PeriodService>;
  let periodProvider: PeriodProvider;
  let loggedInState$: BehaviorSubject<boolean>;

  beforeEach(() => {
    loggedInState$ = new BehaviorSubject<boolean>(false);
    routerSpy = createRouterSpy();
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getLoggedInState",
      "getUserProfile",
      "logout",
    ]);
    authServiceSpy.getLoggedInState.and.returnValue(
      loggedInState$.asObservable(),
    );
    periodServiceSpy = jasmine.createSpyObj<PeriodService>("PeriodService", [
      "getAll",
    ]);
    periodProvider = new PeriodProvider();

    TestBed.overrideComponent(NavMenuComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [NavMenuComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: PeriodService, useValue: periodServiceSpy },
        { provide: PeriodProvider, useValue: periodProvider },
      ],
    });
    fixture = TestBed.createComponent(NavMenuComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it("ngOnInit should load user profile and slice periods up to active period when logged in", () => {
    const p1 = new PeriodModel({
      id: 1,
      description: "Past",
      from: null,
      until: null,
      isActive: false,
    });
    const p2 = new PeriodModel({
      id: 2,
      description: "Active",
      from: null,
      until: null,
      isActive: true,
    });
    const p3 = new PeriodModel({
      id: 3,
      description: "Future",
      from: null,
      until: null,
      isActive: false,
    });

    authServiceSpy.getUserProfile.and.returnValue(
      of(
        new UserProfile({
          id: 1,
          email: "u@pxl.be",
          roles: [],
          teams: [],
          isAuthenticated: true,
        }),
      ),
    );
    periodServiceSpy.getAll.and.returnValue(
      of(GetResult.success([p1, p2, p3])),
    );

    component.ngOnInit();
    loggedInState$.next(true);

    expect(component.allPeriods).toEqual([p1, p2]);
    expect(component.selectedPeriod).toBe(p2);

    component.selectPeriod(p1);
    expect(component.selectedPeriod).toBe(p1);

    spyOn(console, "log");
    component.logout();
    expect(authServiceSpy.logout).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(["/"]);
  });
});

