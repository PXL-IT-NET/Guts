import { Component, OnInit, Input } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { LoginModel } from '../../viewmodels/login.model';


@Component({
    templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
    public model: LoginModel;
    public loading = false;
    public error = '';

    constructor(
        private router: Router,
        private authenticationService: AuthService) {
        this.model = {
            email: '',
            password: ''
        };
    }

    ngOnInit() {
        // reset login status
        this.authenticationService.logout();
    }

    login() {
        this.loading = true;
        this.authenticationService.login(this.model)
            .subscribe(result => {
                if (result.success) {
                    // login successful
                    this.router.navigate(['/']); 
                } else {
                    // login failed
                    this.error = result.message || 'Wrong email-password combination';
                }
                this.loading = false;
            });
    }
}