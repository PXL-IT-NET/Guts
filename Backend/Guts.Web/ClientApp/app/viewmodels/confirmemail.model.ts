export class ConfirmEmailModel {
    userId: string;
    token: string;

    constructor() {
        this.userId = '';
        this.token = '';
    }
}