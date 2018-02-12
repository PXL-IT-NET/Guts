import { HttpResponse } from '@angular/common/http';

export class Result {
    public success: boolean;
    public message?: string;

    constructor() {
        this.success = false;
    }

    public static success(): Result {
        return {
            success: true
        };
    }

    public static fromHttpResponse(response: HttpResponse<any>): Result {

        var result: Result = {
            success: response.ok
        };

        if (result.success) return result;

        let message = response.body || '';
        var messageContainer = this.tryParseJson(message);
        if (messageContainer) {
            message = '';
            for (var propertyName in messageContainer) {
                if (messageContainer.hasOwnProperty(propertyName)) {
                    if (propertyName === '0') {
                        message = <string>messageContainer;
                    }
                    else if (messageContainer[propertyName] instanceof Array) {
                        var lines: string[] = messageContainer[propertyName];
                        for (var line of lines) {
                            message += line + '\n';
                        }
                    }
                }
            }
        }
        result.message = message;
        return result;
    }

    private static tryParseJson(json: string): any {
        try {
            return JSON.parse(json);
        } catch (e) {
            return null;
        }
    }
}