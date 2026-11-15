import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import { AppComponent } from "./app.component";
import { AuthService } from "../services/auth.service";

describe("AppComponent", () => {
  let component: AppComponent;
  let fixture: ComponentFixture<AppComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "isImpersonating",
      "stopImpersonation",
      "impersonatedProfile",
    ]);
    authServiceSpy.isImpersonating.and.returnValue(false);
    authServiceSpy.impersonatedProfile.and.returnValue(null);
    routerSpy = jasmine.createSpyObj<Router>("Router", ["navigate"]);

    await TestBed.configureTestingModule({
      declarations: [AppComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
  });

  it("should stop impersonating and navigate to /home", () => {
    component.stopImpersonating();
    expect(authServiceSpy.stopImpersonation).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);
  });
});
