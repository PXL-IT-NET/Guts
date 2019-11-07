import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from "@angular/common/http";
import { Observable } from "rxjs";
import { ClientSettingsService } from "../services/client.settings.service";
import { Injector, Injectable } from "@angular/core";
import { ClientSettings } from "../services/client.settings";

@Injectable()
export class RelativeInterceptor implements HttpInterceptor{
    private settingsService: ClientSettingsService;

    constructor(private injector: Injector) {
        this.settingsService = injector.get(ClientSettingsService);
      }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

        //do not modify request to an absolute url or to retrieve client settings
        let url = req.url.toLowerCase();
        if(url.startsWith('http') || url.indexOf('api/clientsettings') >= 0){
            return next.handle(req);
        }

        return this.settingsService.get().mergeMap<ClientSettings,HttpEvent<any>>((settings: ClientSettings) => {
           const modifiedRequest = req.clone({ url: settings.apiBaseUrl + req.url})
           return next.handle(modifiedRequest);
        });
    }

}