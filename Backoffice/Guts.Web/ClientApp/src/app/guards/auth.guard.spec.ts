import { Router } from "@angular/router";
import { LocalStorageKeys } from "../util/localstorage.keys";
import { AuthGuard } from "./auth.guard";
import { createRouterSpy } from "../testing/test-helpers";

describe("AuthGuard", () => {
  let guard: AuthGuard;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    routerSpy = createRouterSpy();
    guard = new AuthGuard(routerSpy);
    localStorage.removeItem(LocalStorageKeys.currentToken);
  });

  afterEach(() => {
    localStorage.removeItem(LocalStorageKeys.currentToken);
  });

  it("canActivate should return true when currentToken is present in localStorage", () => {
    localStorage.setItem(LocalStorageKeys.currentToken, '{"token":"jwt"}');

    expect(guard.canActivate()).toBeTrue();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it("canActivate should navigate to /login and return false when currentToken is missing", () => {
    expect(guard.canActivate()).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(["/login"]);
  });
});

