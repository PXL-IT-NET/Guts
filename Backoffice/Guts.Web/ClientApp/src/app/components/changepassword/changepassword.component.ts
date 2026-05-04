import { ChangeDetectorRef, Component } from "@angular/core";
import { AuthService } from "../../services/auth.service";
import { ChangePasswordModel } from "../../viewmodels/changepassword.model";

@Component({
  standalone: false,
  templateUrl: "./changepassword.component.html",
})
export class ChangePasswordComponent {
  public error: string;
  public success: boolean;
  public loading: boolean;
  public model: ChangePasswordModel;

  constructor(
    private authenticationService: AuthService,
    private cdr: ChangeDetectorRef,
  ) {
    this.error = "";
    this.success = false;
    this.loading = false;
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
        this.cdr.detectChanges();
      });
  }
}
