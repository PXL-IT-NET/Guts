import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { map, catchError } from "rxjs/operators";
import { of } from "rxjs";
import { AuthService } from "../services/auth.service";
import { LocalStorageKeys } from "../util/localstorage.keys";

export const adminGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authService = inject(AuthService);

  if (
    !localStorage.getItem(LocalStorageKeys.currentToken) ||
    authService.isImpersonating()
  ) {
    void router.navigate(["/home"]);
    return false;
  }

  return authService.getUserProfile().pipe(
    map((profile) => {
      if (profile.isAdmin() && !profile.isImpersonating) {
        return true;
      }
      void router.navigate(["/home"]);
      return false;
    }),
    catchError(() => {
      void router.navigate(["/home"]);
      return of(false);
    }),
  );
};

export const lectorOrAdminGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authService = inject(AuthService);

  if (
    !localStorage.getItem(LocalStorageKeys.currentToken) ||
    authService.isImpersonating()
  ) {
    void router.navigate(["/home"]);
    return false;
  }

  return authService.getUserProfile().pipe(
    map((profile) => {
      if ((profile.isAdmin() || profile.isLector()) && !profile.isImpersonating) {
        return true;
      }
      void router.navigate(["/home"]);
      return false;
    }),
    catchError(() => {
      void router.navigate(["/home"]);
      return of(false);
    }),
  );
};
