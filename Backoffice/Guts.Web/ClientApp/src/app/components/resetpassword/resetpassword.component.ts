import { Component, OnInit, ChangeDetectionStrategy, signal } from "@angular/core";
import { Params, ActivatedRoute } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { ResetPasswordModel } from "../../viewmodels/resetpassword.model";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./resetpassword.component.html",
})
export class ResetPasswordComponent implements OnInit {
  private _error = signal<string>("");
  public get error(): string {
    return this._error();
  }
  public set error(v: string) {
    this._error.set(v);
  }

  private _success = signal<boolean>(false);
  public get success(): boolean {
    return this._success();
  }
  public set success(v: boolean) {
    this._success.set(v);
  }

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public model: ResetPasswordModel;

  constructor(
    private activatedRoute: ActivatedRoute,
    private authenticationService: AuthService,
  ) {
    this.model = {
      userId: "",
      token: "",
      password: "",
      repeatPassword: "",
    };
  }

  ngOnInit() {
    this.activatedRoute.queryParams.subscribe((params: Params) => {
      this.model.userId = params["userId"];
      this.model.token = params["token"];
    });
  }

  public resetPassword(): void {
    if (this.model.password !== this.model.repeatPassword) {
      this.error = "Please make sure all fields are filled in correctly.";
      return;
    }

    this.loading = true;
    this.authenticationService.resetPassword(this.model).subscribe((result) => {
      if (result.success) {
        this.success = true;
      } else {
        this.error = result.message || "Resetting password failed";
      }
      this.loading = false;
    });
  }
}
