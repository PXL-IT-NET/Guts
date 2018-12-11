import { HttpErrorResponse } from '@angular/common/http';

export class Result {
  public success: boolean;
  public isAuthenticated: boolean;
  public message?: string;

  constructor() {
    this.success = false;
    this.isAuthenticated = true;
  }

  public static fromHttpErrorResponse(response: HttpErrorResponse): Result {

    var result: Result = {
      success: false,
      isAuthenticated: true
    };

    let message = '';

    if (response.status == 401) {
      result.isAuthenticated = false;
    } else if (response.status >= 500) {
      message = "There is a technical problem with the Guts server. (status: " + response.status + " " + response.statusText + ")";
      console.log("API error:");
      console.log(response);
    } else if (response.error instanceof Object) {
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

      if (message == '') {
        message = response.statusText || 'Unknown error';
      }
    } else if (response.error instanceof String) {
      message = response.error as string;
    }

    result.message = message;
    return result;
  }
}

export class PostResult extends Result {

  public static success(): PostResult {
    var result = new PostResult();
    result.success = true;
    return result;
  }

  public static fromHttpErrorResponse(response: HttpErrorResponse): PostResult {
    return Result.fromHttpErrorResponse(response);
  }
}

export class GetResult<T> extends Result {
  public value: T;

  public static success<T>(value: T): GetResult<T> {
    var result = new GetResult<T>();
    result.success = true;
    result.value = value;
    return result;
  }

  public static fromHttpErrorResponse<T>(response: HttpErrorResponse): GetResult<T> {
    var result = Result.fromHttpErrorResponse(response);
    var getResult = new GetResult<T>();
    getResult.success = result.success;
    getResult.message = result.message;
    return getResult;
  }
}
