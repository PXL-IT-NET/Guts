export class RegisterModel {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  repeatpassword: string;
  captchaToken: string;

  constructor() {
    this.firstName = '';
    this.lastName = '';
    this.email = '';
    this.password = '';
    this.repeatpassword = '';
    this.captchaToken = '';
  }
}
