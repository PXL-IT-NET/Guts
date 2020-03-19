export class LoginModel {
  email: string;
  password: string;
  loginSessionPublicIdentifier: string;

  constructor() {
    this.email = '';
    this.password = '';
    this.loginSessionPublicIdentifier = null;
  }
}
