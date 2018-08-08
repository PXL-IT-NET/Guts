export class ForgotPasswordModel {
    email: string;
    captchaToken: string;

    constructor() {
        this.email = '';
        this.captchaToken = '';
    }
}