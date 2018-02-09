import { Response } from '@angular/http';

export class Result {
    public success: boolean;
    public message?: string;

    public static fromHttpResponse(response: Response): Result {

        var result: Result = {
            success: response.ok
        };

        if (result.success) return result;

        var message = response.text();
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