import { Component, ViewChild } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { ForgotPasswordModel } from '../../viewmodels/forgotpassword.model';
import { RecaptchaComponent } from 'ng-recaptcha';

@Component({
    templateUrl: './forgotpassword.component.html'
})
export class ForgotPasswordComponent {
    public model: ForgotPasswordModel;
    public loading = false;
    public error = '';
    public success = false;

    @ViewChild(RecaptchaComponent) public captcha?: RecaptchaComponent;

    constructor(private authenticationService: AuthService) {
        this.model = {
            email: '',
            captchaToken: ''
        };
    }

    public sendMail(): void {
        if (!this.model.captchaToken || this.model.captchaToken === '') {
            this.error = 'Please proof that you are not a robot';
            return;
        }

        this.loading = true;
        this.authenticationService.sendForgotPasswordMail(this.model)
            .subscribe(result => {
                if (result.success) {
                    // registration successful
                    this.success = true;
                } else {
                    // sending mail failed
                    this.error = result.message || 'Sending reset password mail failed';
                    if(this.captcha) this.captcha.reset();
                }
                this.loading = false;
            });
    }

    public resolved(token: string) {
        this.model.captchaToken = token;
    }
}