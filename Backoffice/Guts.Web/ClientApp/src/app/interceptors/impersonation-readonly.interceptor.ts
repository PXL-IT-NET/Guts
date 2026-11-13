import { inject } from "@angular/core";
import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { throwError } from "rxjs";
import { AuthService } from "../services/auth.service";

export const impersonationReadOnlyInterceptor: HttpInterceptorFn = (
  req,
  next,
) => {
  const authService = inject(AuthService);
  const method = req.method.toUpperCase();
  const isReadMethod =
    method === "GET" || method === "HEAD" || method === "OPTIONS";

  if (authService.isImpersonating() && !isReadMethod) {
    return throwError(
      () =>
        new HttpErrorResponse({
          status: 403,
          statusText: "Forbidden",
          error: {
            message:
              "Read-only impersonation mode: state-changing actions are disabled.",
          },
        }),
    );
  }

  return next(req);
};
