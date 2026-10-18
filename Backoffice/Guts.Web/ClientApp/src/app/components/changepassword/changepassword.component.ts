import { Component, ChangeDetectionStrategy, signal } from "@angular/core";
import { AuthService } from "../../services/auth.service";
import { ChangePasswordModel } from "../../viewmodels/changepassword.model";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./changepassword.component.html",
})
export class ChangePasswordComponent {
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

  public model: ChangePasswordModel;

  constructor(private authenticationService: AuthService) {
    this.model = new ChangePasswordModel();
  }

  public changePassword(): void {
    if (this.model.newPassword !== this.model.repeatNewPassword) {
      this.error = "Please make sure all fields are filled in correctly.";
      return;
    }

    this.loading = true;
    this.error = "";

    this.authenticationService
      .changePassword(this.model)
      .subscribe((result) => {
        if (result.success) {
          this.success = true;
          this.model = new ChangePasswordModel();
        } else {
          this.success = false;
          this.error = result.message || "Changing password failed";
        }

        this.loading = false;
      });
  }
}
