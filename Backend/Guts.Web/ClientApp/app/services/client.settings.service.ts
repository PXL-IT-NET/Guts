import { Injectable } from '@angular/core';
import { Http, Response } from '@angular/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/map'
import { ClientSettings } from './client.settings';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../util/localstorage.keys';

@Injectable()
export class ClientSettingsService {

    constructor(private http: Http, private localStorageService: LocalStorageService) {
    }

    public get(): Observable<ClientSettings> {
        var settings: ClientSettings = JSON.parse(String(this.localStorageService.get(LocalStorageKeys.clientSettings)));
        if (settings && settings.apiBaseUrl) {
            console.log("retrieved settings from cache");
            return Observable.from([settings]);
        } else {
            return this.http.get('/api/clientsettings')
                .map((response: Response) => {
                    // login successful if there's a jwt token in the response
                    console.log("retrieved settings from api");
                    let settings: ClientSettings = response.json();
                    this.localStorageService.set(LocalStorageKeys.clientSettings, JSON.stringify(settings));
                    return settings;
                });
        }
    }
}