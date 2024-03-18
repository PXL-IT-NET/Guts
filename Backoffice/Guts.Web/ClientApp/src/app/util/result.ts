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
    } else if (response.status >= 500 || response.error instanceof ProgressEvent) {
      message = "There is a technical problem with the Guts server. (status: " + response.status + " " + response.statusText + ")";
      console.log("API server error:");
      console.log(response);
    } else if (response.error instanceof Object) {
      var messageContainer = response.error as Object;
      for (var propertyName in messageContainer) {
        if (messageContainer.hasOwnProperty(propertyName)) {
          if (propertyName === '0') {
            message = <string>messageContainer;
          }
          else if (typeof (messageContainer[propertyName]) == 'string') {
            message = <string>messageContainer[propertyName];
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
        message = JSON.stringify(response.error) || 'Unknown error';
      }

      console.log("API client error:");
      console.log(response);

    } else if (response.error instanceof String) {
      message = response.error as string;

      console.log("API client error:");
      console.log(response);
    }

    result.message = message;
    return result;
  }
}

export class PostResult extends Result {
  constructor() {
    super();
  }

  public static success(): PostResult {
    var result = new PostResult();
    result.success = true;
    return result;
  }

  public static fromHttpErrorResponse(response: HttpErrorResponse): PostResult {
    return Result.fromHttpErrorResponse(response);
  }
}

export class CreateResult<T> extends Result {
  public value: T;

  constructor() {
    super();
  }

  public static success<T>(value: T): CreateResult<T> {
    var result = new CreateResult<T>();
    result.success = true;
    result.value = value;
    return result;
  }

  public static fromHttpErrorResponse<T>(response: HttpErrorResponse): CreateResult<T> {
    var result = Result.fromHttpErrorResponse(response);
    var createResult = new CreateResult<T>();
    createResult.success = result.success;
    createResult.message = result.message;
    return createResult;
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
