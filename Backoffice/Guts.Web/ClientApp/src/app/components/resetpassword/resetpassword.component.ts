import { Component, OnInit } from '@angular/core';
import { Params, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ResetPasswordModel } from '../../viewmodels/resetpassword.model';


@Component({
    templateUrl: './resetpassword.component.html'
})
export class ResetPasswordComponent implements OnInit {

    public error: string;
    public success: boolean;
    public loading: boolean;
    public model: ResetPasswordModel;

    constructor(
        private activatedRoute: ActivatedRoute,
        private authenticationService: AuthService) {
        this.error = '';
        this.success = false;
        this.loading = false;
        this.model = {
            userId: '',
            token: '',
            password: '',
            repeatPassword: ''
        }
    }

    ngOnInit() {
        this.activatedRoute.queryParams.subscribe((params: Params) => {
            this.model.userId = params['userId'];
            this.model.token = params['token'];
        });
    }

    public resetPassword(): void {
        if (this.model.password !== this.model.repeatPassword) {
            this.error = 'Please make sure all fields are filled in correctly.';
            return;
        }

        this.loading = true;
        this.authenticationService.resetPassword(this.model)
            .subscribe(result => {
                if (result.success) {
                    this.success = true;
                } else {
                    this.error = result.message || 'Resetting password failed';
                }
                this.loading = false;
            });
    }
}