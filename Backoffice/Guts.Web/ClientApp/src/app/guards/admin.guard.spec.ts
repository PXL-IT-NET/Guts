import { TestBed } from "@angular/core/testing";
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from "@angular/router";
import { Observable, of, throwError } from "rxjs";
import { adminGuard, lectorOrAdminGuard } from "./admin.guard";
import { AuthService } from "../services/auth.service";
import { LocalStorageKeys } from "../util/localstorage.keys";
import { UserProfile } from "../viewmodels/user.model";

describe("adminGuard and lectorOrAdminGuard", () => {
  let routerSpy: jasmine.SpyObj<Router>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  beforeEach(() => {
    localStorage.removeItem(LocalStorageKeys.currentToken);
    routerSpy = jasmine.createSpyObj<Router>("Router", ["navigate"]);
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "isImpersonating",
      "getUserProfile",
    ]);
    authServiceSpy.isImpersonating.and.returnValue(false);

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authServiceSpy },
      ],
    });
  });

  afterEach(() => {
    localStorage.removeItem(LocalStorageKeys.currentToken);
  });

  function runGuard(
    guardFn: typeof adminGuard,
  ): boolean | Observable<boolean> {
    return TestBed.runInInjectionContext(() => guardFn(route, state)) as
      | boolean
      | Observable<boolean>;
  }

  describe("adminGuard", () => {
    it("should redirect to /home and return false when no token is stored", () => {
      const result = runGuard(adminGuard);
      expect(result).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);
    });

    it("should redirect to /home and return false when impersonating", () => {
      localStorage.setItem(LocalStorageKeys.currentToken, "tok");
      authServiceSpy.isImpersonating.and.returnValue(true);

      const result = runGuard(adminGuard);
      expect(result).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);
    });

    it("should return true when profile is admin and not impersonating", (done) => {
      localStorage.setItem(LocalStorageKeys.currentToken, "tok");
      authServiceSpy.getUserProfile.and.returnValue(
        of(
          new UserProfile({
            id: 1,
            email: "admin@pxl.be",
            roles: ["admin"],
            teams: [],
            isAuthenticated: true,
            isImpersonating: false,
          }),
        ),
      );

      (runGuard(adminGuard) as Observable<boolean>).subscribe((allowed) => {
        expect(allowed).toBeTrue();
        expect(routerSpy.navigate).not.toHaveBeenCalled();
        done();
      });
    });

    it("should redirect and return false when profile is non-admin or impersonating", (done) => {
      localStorage.setItem(LocalStorageKeys.currentToken, "tok");
      authServiceSpy.getUserProfile.and.returnValue(
        of(
          new UserProfile({
            id: 2,
            email: "student@pxl.be",
            roles: ["student"],
            teams: [],
            isAuthenticated: true,
            isImpersonating: false,
          }),
        ),
      );

      (runGuard(adminGuard) as Observable<boolean>).subscribe((allowed) => {
        expect(allowed).toBeFalse();
        expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);
        done();
      });
    });

    it("should redirect and return false when getUserProfile errors", (done) => {
      localStorage.setItem(LocalStorageKeys.currentToken, "tok");
      authServiceSpy.getUserProfile.and.returnValue(
        throwError(() => new Error("Network error")),
      );

      (runGuard(adminGuard) as Observable<boolean>).subscribe((allowed) => {
        expect(allowed).toBeFalse();
        expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);
        done();
      });
    });
  });

  describe("lectorOrAdminGuard", () => {
    it("should redirect to /home and return false when no token or impersonating", () => {
      expect(runGuard(lectorOrAdminGuard)).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);

      routerSpy.navigate.calls.reset();
      localStorage.setItem(LocalStorageKeys.currentToken, "tok");
      authServiceSpy.isImpersonating.and.returnValue(true);
      expect(runGuard(lectorOrAdminGuard)).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);
    });

    it("should return true for lector or admin when not impersonating", (done) => {
      localStorage.setItem(LocalStorageKeys.currentToken, "tok");
      authServiceSpy.getUserProfile.and.returnValue(
        of(
          new UserProfile({
            id: 3,
            email: "lector@pxl.be",
            roles: ["lector"],
            teams: [],
            isAuthenticated: true,
            isImpersonating: false,
          }),
        ),
      );

      (runGuard(lectorOrAdminGuard) as Observable<boolean>).subscribe(
        (allowed) => {
          expect(allowed).toBeTrue();
          done();
        },
      );
    });

    it("should redirect and return false for student and on error", (done) => {
      localStorage.setItem(LocalStorageKeys.currentToken, "tok");
      authServiceSpy.getUserProfile.and.returnValue(
        of(
          new UserProfile({
            id: 4,
            email: "student@pxl.be",
            roles: ["student"],
            teams: [],
            isAuthenticated: true,
            isImpersonating: false,
          }),
        ),
      );

      (runGuard(lectorOrAdminGuard) as Observable<boolean>).subscribe(
        (allowed) => {
          expect(allowed).toBeFalse();
          expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);

          routerSpy.navigate.calls.reset();
          authServiceSpy.getUserProfile.and.returnValue(
            throwError(() => new Error("401")),
          );
          (runGuard(lectorOrAdminGuard) as Observable<boolean>).subscribe(
            (errAllowed) => {
              expect(errAllowed).toBeFalse();
              expect(routerSpy.navigate).toHaveBeenCalledWith(["/home"]);
              done();
            },
          );
        },
      );
    });
  });
});
