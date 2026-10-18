import { inject, Injectable } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { LocalStorageKeys } from "../util/localstorage.keys";

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  if (localStorage.getItem(LocalStorageKeys.currentToken)) {
    return true;
  }

  void router.navigate(["/login"]);
  return false;
};

@Injectable({
  providedIn: "root",
})
export class AuthGuard {
  constructor(private router: Router) {}

  canActivate() {
    if (localStorage.getItem(LocalStorageKeys.currentToken)) {
      // logged in so return true
      return true;
    }

    // not logged in so redirect to login page
    void this.router.navigate(["/login"]);
    return false;
  }
}

