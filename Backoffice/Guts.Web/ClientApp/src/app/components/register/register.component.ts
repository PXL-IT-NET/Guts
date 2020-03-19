import { Component, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { RegisterModel } from '../../viewmodels/register.model';
import { RecaptchaComponent } from 'ng-recaptcha';

@Component({
  templateUrl: './register.component.html'
})
export class RegisterComponent {
  public model: RegisterModel;
  public loading = false;
  public error = '';
  public registered = false;

  @ViewChild(RecaptchaComponent, {static: false}) public captcha?: RecaptchaComponent;

  constructor(
    private router: Router,
    private authenticationService: AuthService) {
    this.model = new RegisterModel();
  }

  public register(): void {
    if (this.model.password !== this.model.repeatpassword) {
      this.error = 'Please make sure all fields are filled in correctly.';
      return;
    }

    if (!this.model.captchaToken || this.model.captchaToken === '') {
      this.error = 'Please proof that you are not a robot';
      return;
    }

    this.loading = true;
    this.authenticationService.register(this.model)
      .subscribe(result => {
        if (result.success) {
          // registration successful
          this.registered = true;
        } else {
          // registration failed
          this.error = result.message || 'Registration failed';
          if (this.captcha) this.captcha.reset();
        }
        this.loading = false;
      });
  }

  public resolved(token: string) {
    this.model.captchaToken = token;
  }
}
