import { HttpErrorResponse } from '@angular/common/http';

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

    public static fromHttpErrorResponse(response: HttpErrorResponse): Result {

        var result: Result = {
            success: false
        };

        let message = '';

        if (response.error instanceof Object) {
            var messageContainer = response.error as Object;
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
        } else if (response.error instanceof String) {
            message = response.error as string;
        }

        result.message = message;
        return result;
    }
}
