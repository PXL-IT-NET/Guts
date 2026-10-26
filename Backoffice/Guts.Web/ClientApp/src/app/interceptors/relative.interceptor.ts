import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptorFn,
} from "@angular/common/http";
import { Observable } from "rxjs";
import { mergeMap } from "rxjs/operators";
import { ClientSettingsService } from "../services/client.settings.service";
import { Injector, Injectable, inject } from "@angular/core";
import { ClientSettings } from "../services/client.settings";

export const relativeInterceptor: HttpInterceptorFn = (req, next) => {
  const url = req.url.toLowerCase();
  if (url.startsWith("http") || url.includes("api/clientsettings")) {
    return next(req);
  }

  const settingsService = inject(ClientSettingsService);
  return settingsService.get().pipe(
    mergeMap((settings: ClientSettings) => {
      const modifiedRequest = req.clone({
        url: settings.apiBaseUrl + req.url,
      });
      return next(modifiedRequest);
    }),
  );
};

@Injectable({
  providedIn: "root",
})
export class RelativeInterceptor implements HttpInterceptor {
  private settingsService: ClientSettingsService;

  constructor(private readonly injector: Injector) {
    this.settingsService = injector.get(ClientSettingsService);
  }

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler,
  ): Observable<HttpEvent<any>> {
    //do not modify request to an absolute url or to retrieve client settings
    let url = req.url.toLowerCase();
    if (url.startsWith("http") || url.includes("api/clientsettings")) {
      return next.handle(req);
    }

    return this.settingsService.get().pipe(
      mergeMap((settings: ClientSettings) => {
        const modifiedRequest = req.clone({
          url: settings.apiBaseUrl + req.url,
        });
        return next.handle(modifiedRequest);
      }),
    );
  }
}
