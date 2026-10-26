import { Injectable, Inject } from "@angular/core";
import { Observable, of } from "rxjs";
import { ClientSettings } from "./client.settings";
import { environment } from "../../environments/environment";

@Injectable({
  providedIn: "root",
})
export class ClientSettingsService {
  constructor(@Inject("BASE_URL") private readonly baseUrl: string) {}

  public get(): Observable<ClientSettings> {
    const settings = new ClientSettings();
    settings.apiBaseUrl = environment.apiBaseUrl;

    return of(settings);
  }
}
