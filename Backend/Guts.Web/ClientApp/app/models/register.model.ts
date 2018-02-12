export class RegisterModel {
    email: string;
    password: string;
    repeatpassword: string;
    captchaToken: string;

    constructor() {
        this.email = '';
        this.password = '';
        this.repeatpassword = '';
        this.captchaToken = '';
    }
}