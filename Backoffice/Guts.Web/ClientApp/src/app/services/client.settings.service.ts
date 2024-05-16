import { Injectable, Inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ClientSettings } from './client.settings';
import { environment } from '../../environments/environment';

@Injectable()
export class ClientSettingsService {

  constructor( @Inject('BASE_URL') private baseUrl: string) {
  }

  public get(): Observable<ClientSettings> {
    //var settings: ClientSettings = JSON.parse(String(this.localStorageService.get(LocalStorageKeys.clientSettings)));
    //if (settings && settings.apiBaseUrl) {
    //  return of(settings);
    //} else {
    //  return this.http.get<ClientSettings>(this.baseUrl + 'api/clientsettings').pipe(
    //    map((settings) => {
    //      this.localStorageService.set(LocalStorageKeys.clientSettings, JSON.stringify(settings));
    //      return settings;
    //    }));
    //}

    var settings = new ClientSettings();
    settings.apiBaseUrl = environment.apiBaseUrl;

    return of(settings);
  }
}
