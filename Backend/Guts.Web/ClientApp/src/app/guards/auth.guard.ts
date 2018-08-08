import { Injectable } from '@angular/core';
import { Router, CanActivate } from '@angular/router';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../util/localstorage.keys';

@Injectable()
export class AuthGuard implements CanActivate {

    constructor(private router: Router, private localStorageService: LocalStorageService) { }

    canActivate() {
        if (this.localStorageService.get(LocalStorageKeys.currentToken)) {
            // logged in so return true
            return true;
        }

        // not logged in so redirect to login page
        this.router.navigate(['/login']);
        return false;
    }
}