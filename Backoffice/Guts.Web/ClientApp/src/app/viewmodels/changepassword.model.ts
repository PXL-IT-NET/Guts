export class ChangePasswordModel {
  public currentPassword: string;
  public newPassword: string;
  public repeatNewPassword: string;

  constructor() {
    this.currentPassword = "";
    this.newPassword = "";
    this.repeatNewPassword = "";
  }
}
