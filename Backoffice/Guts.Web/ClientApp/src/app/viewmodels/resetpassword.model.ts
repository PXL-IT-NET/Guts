export class ResetPasswordModel {
    userId: string;
    token: string;
    password: string;
    repeatPassword: string;

    constructor() {
        this.userId = '';
        this.token = '';
        this.password = '';
        this.repeatPassword = '';
    }
}