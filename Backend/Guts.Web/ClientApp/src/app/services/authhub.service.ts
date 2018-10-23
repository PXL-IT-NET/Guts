import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import * as signalR from "@aspnet/signalr";


@Injectable()
export class AuthHubService {
  private _connection: signalR.HubConnection;

  constructor(private settingsService: ClientSettingsService) {

    this._connection = null;
  }

  private get connectionObservable(): Observable<signalR.HubConnection> {
    if (!this._connection) {
      return this.settingsService.get().mergeMap<ClientSettings, signalR.HubConnection>((settings: ClientSettings) => {
        this._connection = new signalR.HubConnectionBuilder()
          .withUrl(settings.apiBaseUrl + 'authHub')
          .build();       
        return [this._connection];
      });
    }
    return Observable.from([this._connection]);
  }

  public sendToken(sessionId: string, token: string): Observable<void> {
    return this.connectionObservable.mergeMap((connection) => {
      var promise = connection.start()
        .then(() => {
          connection.send('SendToken', sessionId, token);
        })
        .then(() => connection.stop())
        .catch(() => connection.stop());
      return Observable.fromPromise(promise);
    });
  }

  public cancel(sessionId: string): Observable<void> {
    return this.connectionObservable.mergeMap((connection) => {
      var promise = connection.start()
        .then(() => {
          connection.send('Cancel', sessionId);
        })
        .then(() => connection.stop())
        .catch(() => connection.stop());
      return Observable.fromPromise(promise);
    });
  }
}
