import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { ClientSettings } from './client.settings';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../util/localstorage.keys';

@Injectable()
export class ClientSettingsService {

  constructor(private http: HttpClient, private localStorageService: LocalStorageService, @Inject('BASE_URL') private baseUrl: string) {
  }

  public get(): Observable<ClientSettings> {
    var settings: ClientSettings = JSON.parse(String(this.localStorageService.get(LocalStorageKeys.clientSettings)));
    if (settings && settings.apiBaseUrl) {
      return of(settings);
    } else {
      return this.http.get<ClientSettings>(this.baseUrl + 'api/clientsettings').pipe(
        map((settings) => {
          this.localStorageService.set(LocalStorageKeys.clientSettings, JSON.stringify(settings));
          return settings;
        }));
    }
  }
}
