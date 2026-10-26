import { HttpErrorResponse } from "@angular/common/http";

export class Result {
  public success: boolean;
  public isAuthenticated: boolean;
  public message?: string;

  constructor() {
    this.success = false;
    this.isAuthenticated = true;
  }

  private static extractMessageFromObject(error: object): string {
    const messageContainer = error as Record<string, any>;
    let message = "";
    for (const propertyName in messageContainer) {
      if (Object.hasOwn(messageContainer, propertyName)) {
        if (propertyName === "0") {
          message = <string>(messageContainer as unknown);
        } else if (typeof messageContainer[propertyName] === "string") {
          message = <string>messageContainer[propertyName];
        } else if (Array.isArray(messageContainer[propertyName])) {
          const lines: string[] = messageContainer[propertyName];
          for (const line of lines) {
            message += line + "\n";
          }
        }
      }
    }
    return message || JSON.stringify(error) || "Unknown error";
  }

  public static fromHttpErrorResponse(response: HttpErrorResponse): Result {
    const result: Result = {
      success: false,
      isAuthenticated: true,
    };

    let message = "";

    if (response.status === 401) {
      result.isAuthenticated = false;
    } else if (
      response.status >= 500 ||
      response.error instanceof ProgressEvent
    ) {
      message =
        "There is a technical problem with the Guts server. (status: " +
        response.status +
        " " +
        response.statusText +
        ")";
      console.log("API server error:");
      console.log(response);
    } else if (
      typeof response.error === "string" ||
      response.error instanceof String
    ) {
      message = response.error.toString();

      console.log("API client error:");
      console.log(response);
    } else if (response.error instanceof Object) {
      message = Result.extractMessageFromObject(response.error);
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
    const result = new PostResult();
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
    const result = new CreateResult<T>();
    result.success = true;
    result.value = value;
    return result;
  }

  public static fromHttpErrorResponse<T>(
    response: HttpErrorResponse,
  ): CreateResult<T> {
    const result = Result.fromHttpErrorResponse(response);
    const createResult = new CreateResult<T>();
    createResult.success = result.success;
    createResult.isAuthenticated = result.isAuthenticated;
    createResult.message = result.message;
    return createResult;
  }
}

export class GetResult<T> extends Result {
  public value: T;

  public static success<T>(value: T): GetResult<T> {
    const result = new GetResult<T>();
    result.success = true;
    result.value = value;
    return result;
  }

  public static fromHttpErrorResponse<T>(
    response: HttpErrorResponse,
  ): GetResult<T> {
    const result = Result.fromHttpErrorResponse(response);
    const getResult = new GetResult<T>();
    getResult.success = result.success;
    getResult.isAuthenticated = result.isAuthenticated;
    getResult.message = result.message;
    return getResult;
  }
}
