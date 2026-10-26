import {
  HttpErrorResponse,
  HttpHandler,
  HttpRequest,
  HttpResponse,
} from "@angular/common/http";
import { Injector } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { of, throwError } from "rxjs";
import { AuthService } from "../services/auth.service";
import { TokenInterceptor, tokenInterceptor } from "./tokeninterceptor";

describe("TokenInterceptor", () => {
  let interceptor: TokenInterceptor;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let nextHandlerSpy: jasmine.SpyObj<HttpHandler>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getToken",
      "clearToken",
    ]);
    nextHandlerSpy = jasmine.createSpyObj<HttpHandler>("HttpHandler", [
      "handle",
    ]);

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authServiceSpy }],
    });

    const injector = Injector.create({
      providers: [{ provide: AuthService, useValue: authServiceSpy }],
    });
    interceptor = new TokenInterceptor(injector);
  });

  it("should add Authorization Bearer header when token is available", () => {
    authServiceSpy.getToken.and.returnValue("secret-jwt");
    nextHandlerSpy.handle.and.returnValue(of(new HttpResponse({ status: 200 })));
    const request = new HttpRequest("GET", "api/courses");

    interceptor.intercept(request, nextHandlerSpy).subscribe();

    const forwardedRequest = nextHandlerSpy.handle.calls.mostRecent().args[0];
    expect(forwardedRequest.headers.get("Authorization")).toBe(
      "Bearer secret-jwt",
    );
  });

  it("should not add Authorization header when token is null", () => {
    authServiceSpy.getToken.and.returnValue(null);
    nextHandlerSpy.handle.and.returnValue(of(new HttpResponse({ status: 200 })));
    const request = new HttpRequest("GET", "api/courses");

    interceptor.intercept(request, nextHandlerSpy).subscribe();

    const forwardedRequest = nextHandlerSpy.handle.calls.mostRecent().args[0];
    expect(forwardedRequest.headers.has("Authorization")).toBeFalse();
  });

  it("should call authService.clearToken() when response fails with 401", () => {
    authServiceSpy.getToken.and.returnValue("expired-jwt");
    const unauthorizedError = new HttpErrorResponse({ status: 401 });
    nextHandlerSpy.handle.and.returnValue(throwError(() => unauthorizedError));
    const request = new HttpRequest("GET", "api/courses");

    interceptor.intercept(request, nextHandlerSpy).subscribe({
      error: () => {},
    });

    expect(authServiceSpy.clearToken).toHaveBeenCalled();
  });

  it("should not clear token when response fails with non-401 status", () => {
    authServiceSpy.getToken.and.returnValue("valid-jwt");
    const badRequestError = new HttpErrorResponse({ status: 400 });
    nextHandlerSpy.handle.and.returnValue(throwError(() => badRequestError));
    const request = new HttpRequest("GET", "api/courses");

    interceptor.intercept(request, nextHandlerSpy).subscribe({
      error: () => {},
    });

    expect(authServiceSpy.clearToken).not.toHaveBeenCalled();
  });

  it("tokenInterceptor function should attach token when available and clear token on 401", () => {
    let forwardedReq: HttpRequest<unknown> | null = null;
    authServiceSpy.getToken.and.returnValue("fn-jwt");

    TestBed.runInInjectionContext(() => {
      tokenInterceptor(new HttpRequest("GET", "api/courses"), (req) => {
        forwardedReq = req;
        return of(new HttpResponse({ status: 200 }));
      }).subscribe();
    });
    expect(forwardedReq!.headers.get("Authorization")).toBe("Bearer fn-jwt");

    authServiceSpy.getToken.and.returnValue(null);
    TestBed.runInInjectionContext(() => {
      tokenInterceptor(new HttpRequest("GET", "api/courses"), (req) => {
        forwardedReq = req;
        return throwError(() => new HttpErrorResponse({ status: 401 }));
      }).subscribe({ error: () => {} });
    });
    expect(forwardedReq!.headers.has("Authorization")).toBeFalse();
    expect(authServiceSpy.clearToken).toHaveBeenCalled();
  });
});
