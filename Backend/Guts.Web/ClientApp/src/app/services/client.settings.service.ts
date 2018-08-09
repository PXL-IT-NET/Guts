import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ClientSettings } from './client.settings';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../util/localstorage.keys';

@Injectable()
export class ClientSettingsService {

    constructor(private http: HttpClient, private localStorageService: LocalStorageService) {
    }

    public get(): Observable<ClientSettings> {
        var settings: ClientSettings = JSON.parse(String(this.localStorageService.get(LocalStorageKeys.clientSettings)));
        if (settings && settings.apiBaseUrl) {
            return Observable.from([settings]);
        } else {
            return this.http.get<ClientSettings>('/api/clientsettings')
                .map((settings) => {
                    this.localStorageService.set(LocalStorageKeys.clientSettings, JSON.stringify(settings));
                    return settings;
                });
        }
    }
}
