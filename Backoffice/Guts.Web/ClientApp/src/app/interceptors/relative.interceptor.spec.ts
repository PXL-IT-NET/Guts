import { HttpHandler, HttpRequest, HttpResponse } from "@angular/common/http";
import { Injector } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { ClientSettings } from "../services/client.settings";
import { ClientSettingsService } from "../services/client.settings.service";
import { RelativeInterceptor, relativeInterceptor } from "./relative.interceptor";

describe("RelativeInterceptor", () => {
  let interceptor: RelativeInterceptor;
  let settingsServiceSpy: jasmine.SpyObj<ClientSettingsService>;
  let nextHandlerSpy: jasmine.SpyObj<HttpHandler>;

  beforeEach(() => {
    settingsServiceSpy = jasmine.createSpyObj<ClientSettingsService>(
      "ClientSettingsService",
      ["get"],
    );
    const settings = new ClientSettings();
    settings.apiBaseUrl = "https://api.guts.pxl.be/";
    settingsServiceSpy.get.and.returnValue(of(settings));

    nextHandlerSpy = jasmine.createSpyObj<HttpHandler>("HttpHandler", [
      "handle",
    ]);
    nextHandlerSpy.handle.and.returnValue(of(new HttpResponse({ status: 200 })));

    TestBed.configureTestingModule({
      providers: [
        { provide: ClientSettingsService, useValue: settingsServiceSpy },
      ],
    });

    const injector = Injector.create({
      providers: [
        { provide: ClientSettingsService, useValue: settingsServiceSpy },
      ],
    });
    interceptor = new RelativeInterceptor(injector);
  });

  it("should prepend apiBaseUrl to relative API requests", () => {
    const request = new HttpRequest("GET", "api/courses");

    interceptor.intercept(request, nextHandlerSpy).subscribe();

    const forwardedRequest = nextHandlerSpy.handle.calls.mostRecent().args[0];
    expect(forwardedRequest.url).toBe("https://api.guts.pxl.be/api/courses");
  });

  it("should not modify absolute HTTP/HTTPS URLs", () => {
    const request = new HttpRequest("GET", "https://external.org/data");

    interceptor.intercept(request, nextHandlerSpy).subscribe();

    expect(settingsServiceSpy.get).not.toHaveBeenCalled();
    const forwardedRequest = nextHandlerSpy.handle.calls.mostRecent().args[0];
    expect(forwardedRequest.url).toBe("https://external.org/data");
  });

  it("should not modify requests to api/clientsettings", () => {
    const request = new HttpRequest("GET", "api/clientsettings");

    interceptor.intercept(request, nextHandlerSpy).subscribe();

    expect(settingsServiceSpy.get).not.toHaveBeenCalled();
    const forwardedRequest = nextHandlerSpy.handle.calls.mostRecent().args[0];
    expect(forwardedRequest.url).toBe("api/clientsettings");
  });

  it("relativeInterceptor function should prepend apiBaseUrl for relative URLs and skip absolute/clientsettings", () => {
    let forwardedUrl = "";
    const nextFn = (req: HttpRequest<unknown>) => {
      forwardedUrl = req.url;
      return of(new HttpResponse({ status: 200 }));
    };

    TestBed.runInInjectionContext(() => {
      relativeInterceptor(new HttpRequest("GET", "api/courses"), nextFn).subscribe();
    });
    expect(forwardedUrl).toBe("https://api.guts.pxl.be/api/courses");

    TestBed.runInInjectionContext(() => {
      relativeInterceptor(new HttpRequest("GET", "https://external.org/x"), nextFn).subscribe();
    });
    expect(forwardedUrl).toBe("https://external.org/x");

    TestBed.runInInjectionContext(() => {
      relativeInterceptor(new HttpRequest("GET", "api/clientsettings"), nextFn).subscribe();
    });
    expect(forwardedUrl).toBe("api/clientsettings");
  });
});
