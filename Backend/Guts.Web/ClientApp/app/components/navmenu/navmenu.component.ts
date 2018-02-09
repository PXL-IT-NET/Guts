import { Component } from '@angular/core';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../../util/localstorage.keys';
import { Router } from '@angular/router';

@Component({
    selector: 'nav-menu',
    templateUrl: './navmenu.component.html',
    styleUrls: ['./navmenu.component.css']
})
export class NavMenuComponent {
    constructor(private localStorageService: LocalStorageService,
        private router: Router) {   
    }

    public isLoggedIn() : boolean {
        if (this.localStorageService.get(LocalStorageKeys.currentToken)) {
           return true;
        } else {
            return false;
        }
    }

    public logout() {
        this.localStorageService.set(LocalStorageKeys.currentToken, null);
        this.router.navigate(['/']);
    }
}
