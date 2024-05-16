import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { LocalStorageKeys } from '../util/localstorage.keys';

@Injectable()
export class AuthGuard  {

    constructor(private router: Router) { }

    canActivate() {
        if (localStorage.getItem(LocalStorageKeys.currentToken)) {
            // logged in so return true
            return true;
        }

        // not logged in so redirect to login page
        this.router.navigate(['/login']);
        return false;
    }
}