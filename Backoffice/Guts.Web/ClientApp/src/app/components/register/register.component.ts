import { Component, ViewChild, ChangeDetectionStrategy, signal } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { RegisterModel } from "../../viewmodels/register.model";
import { RecaptchaDirective } from "../../directives/recaptcha.directive";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./register.component.html",
})
export class RegisterComponent {
  public model: RegisterModel;

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private _error = signal<string>("");
  public get error(): string {
    return this._error();
  }
  public set error(v: string) {
    this._error.set(v);
  }

  private _registered = signal<boolean>(false);
  public get registered(): boolean {
    return this._registered();
  }
  public set registered(v: boolean) {
    this._registered.set(v);
  }

  @ViewChild(RecaptchaDirective, { static: false })
  public captcha?: RecaptchaDirective;

  constructor(
    private router: Router,
    private authenticationService: AuthService,
  ) {
    this.model = new RegisterModel();
  }

  public register(): void {
    if (this.model.password !== this.model.repeatpassword) {
      this.error = "Please make sure all fields are filled in correctly.";
      return;
    }

    if (!this.model.captchaToken || this.model.captchaToken === "") {
      this.error = "Please proof that you are not a robot";
      return;
    }

    this.loading = true;
    this.authenticationService.register(this.model).subscribe((result) => {
      if (result.success) {
        // registration successful
        this.registered = true;
      } else {
        // registration failed
        this.error = result.message || "Registration failed";
        if (this.captcha) this.captcha.reset();
      }
      this.loading = false;
    });
  }

  public resolved(token: string) {
    this.model.captchaToken = token;
  }
}
