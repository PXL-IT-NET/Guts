import { HttpErrorResponse } from "@angular/common/http";
import { CreateResult, GetResult, PostResult, Result } from "./result";

describe("Result utilities", () => {
  beforeEach(() => {
    spyOn(console, "log");
  });

  describe("Result", () => {
    it("should initialize with success=false and isAuthenticated=true", () => {
      const result = new Result();
      expect(result.success).toBeFalse();
      expect(result.isAuthenticated).toBeTrue();
    });

    it("fromHttpErrorResponse should mark isAuthenticated=false on 401", () => {
      const response = new HttpErrorResponse({
        status: 401,
        statusText: "Unauthorized",
      });

      const result = Result.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.isAuthenticated).toBeFalse();
      expect(result.message).toBe("");
    });

    it("fromHttpErrorResponse should return server error message on 500+", () => {
      const response = new HttpErrorResponse({
        status: 503,
        statusText: "Service Unavailable",
      });

      const result = Result.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.isAuthenticated).toBeTrue();
      expect(result.message).toContain(
        "There is a technical problem with the Guts server. (status: 503 Service Unavailable)",
      );
    });

    it("fromHttpErrorResponse should return server error message on ProgressEvent error", () => {
      const response = new HttpErrorResponse({
        status: 0,
        statusText: "Unknown Error",
        error: new ProgressEvent("error"),
      });

      const result = Result.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.message).toContain(
        "There is a technical problem with the Guts server.",
      );
    });

    it("fromHttpErrorResponse should extract string property from error object", () => {
      const response = new HttpErrorResponse({
        status: 400,
        statusText: "Bad Request",
        error: { message: "Invalid input data" },
      });

      const result = Result.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.message).toBe("Invalid input data");
    });

    it("fromHttpErrorResponse should concatenate array property messages from error object", () => {
      const response = new HttpErrorResponse({
        status: 400,
        statusText: "Bad Request",
        error: { Password: ["Too short", "Requires digit"] },
      });

      const result = Result.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.message).toBe("Too short\nRequires digit\n");
    });

    it("fromHttpErrorResponse should stringify empty error object", () => {
      const response = new HttpErrorResponse({
        status: 400,
        statusText: "Bad Request",
        error: {},
      });

      const result = Result.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.message).toBe("{}");
    });

    it("fromHttpErrorResponse should extract message when error is a String object", () => {
      const response = new HttpErrorResponse({
        status: 400,
        statusText: "Bad Request",
        // eslint-disable-next-line no-new-wrappers
        error: new String("String error message"),
      });

      const result = Result.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.message).toBe("String error message");
    });
  });

  describe("PostResult", () => {
    it("success should return PostResult with success=true", () => {
      const result = PostResult.success();
      expect(result.success).toBeTrue();
      expect(result.isAuthenticated).toBeTrue();
    });

    it("fromHttpErrorResponse should delegate to Result.fromHttpErrorResponse", () => {
      const response = new HttpErrorResponse({
        status: 401,
        statusText: "Unauthorized",
      });

      const result = PostResult.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.isAuthenticated).toBeFalse();
    });
  });

  describe("CreateResult", () => {
    it("success should return CreateResult with value and success=true", () => {
      const payload = { id: 10, name: "Item" };
      const result = CreateResult.success(payload);

      expect(result.success).toBeTrue();
      expect(result.value).toEqual(payload);
    });

    it("fromHttpErrorResponse should copy failure state and message", () => {
      const response = new HttpErrorResponse({
        status: 400,
        statusText: "Bad Request",
        error: { error: "Creation failed" },
      });

      const result = CreateResult.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.message).toBe("Creation failed");
    });

    it("fromHttpErrorResponse should mark isAuthenticated=false on 401", () => {
      const response = new HttpErrorResponse({
        status: 401,
        statusText: "Unauthorized",
      });

      const result = CreateResult.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.isAuthenticated).toBeFalse();
    });
  });

  describe("GetResult", () => {
    it("success should return GetResult with value and success=true", () => {
      const result = GetResult.success([1, 2, 3]);

      expect(result.success).toBeTrue();
      expect(result.value).toEqual([1, 2, 3]);
    });

    it("fromHttpErrorResponse should copy failure state and message", () => {
      const response = new HttpErrorResponse({
        status: 400,
        statusText: "Bad Request",
        error: { error: "Fetch failed" },
      });

      const result = GetResult.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.message).toBe("Fetch failed");
    });

    it("fromHttpErrorResponse should mark isAuthenticated=false on 401", () => {
      const response = new HttpErrorResponse({
        status: 401,
        statusText: "Unauthorized",
      });

      const result = GetResult.fromHttpErrorResponse(response);

      expect(result.success).toBeFalse();
      expect(result.isAuthenticated).toBeFalse();
    });
  });
});

