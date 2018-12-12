import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Subject } from 'rxjs/Subject';
import { LoginModel } from '../viewmodels/login.model';
import { TokenModel } from '../viewmodels/token.model';
import { RegisterModel } from '../viewmodels/register.model';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../util/localstorage.keys';
import { PostResult } from "../util/Result";
import { ConfirmEmailModel } from '../viewmodels/confirmemail.model';
import { ForgotPasswordModel } from '../viewmodels/forgotpassword.model';
import { ResetPasswordModel } from '../viewmodels/resetpassword.model';

@Injectable()
export class AuthService {
  private apiBaseUrl: string;
  private tokenModel: TokenModel | null;
  private loggedInState: Subject<boolean>;

  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService,
    private localStorageService: LocalStorageService) {

    this.loggedInState = new Subject<boolean>();
    this.apiBaseUrl = '';

    // set token if saved in local storage
    this.tokenModel = JSON.parse(String(this.localStorageService.get(LocalStorageKeys.currentToken)));
  }

  public getToken(): string | null {
    if (this.tokenModel) {
      return this.tokenModel.token;
    }
    return null;
  }

  public clearToken(): void {
    this.tokenModel = null;
    this.localStorageService.remove(LocalStorageKeys.currentToken);
  }

  public login(model: LoginModel): Observable<PostResult> {

    return this.settingsService.get().mergeMap<ClientSettings, PostResult>((settings: ClientSettings) => {

      return this.http.post(settings.apiBaseUrl + 'api/auth/token', model)
        .map((object: Object) => {
          var tokenModel = object as TokenModel;
          if (tokenModel && tokenModel.token) {
            // set token property
            this.tokenModel = tokenModel;

            // store username and jwt token in local storage to keep user logged in between page refreshes
            this.localStorageService.set(LocalStorageKeys.currentToken, JSON.stringify(tokenModel));

            // return true to indicate successful login
            this.loggedInState.next(true);
            return PostResult.success();
          } else {
            // return false to indicate failed login
            this.loggedInState.next(false);
            var result = new PostResult();
            result.isAuthenticated = false;
            result.message = 'No token present in returned token model';
            return result;
          }
        }).catch((errorResponse: HttpErrorResponse) => {
          this.loggedInState.next(false);
          return Observable.from([PostResult.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }

  public cancelLoginSession(loginSessionPublicIdentifier: string): Observable<PostResult> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.patch(settings.apiBaseUrl + 'api/auth/loginsession/' + loginSessionPublicIdentifier + '/cancel',
        null).map<Object, PostResult>(() => PostResult.success());
    });
  }

  public getLoggedInState(): Observable<boolean> {
    return this.loggedInState.asObservable();
  }

  public logout(): void {
    // clear token remove user from local storage to log user out
    this.tokenModel = null;
    this.localStorageService.remove(LocalStorageKeys.currentToken);
  }

  public register(model: RegisterModel): Observable<PostResult> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/register', model)
        .map<Object, PostResult>(() => {
          return PostResult.success();
        })
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([PostResult.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }

  public confirmEmail(model: ConfirmEmailModel): Observable<PostResult> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/confirmemail', model)
        .map<Object, PostResult>(() => {
          return PostResult.success();
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([PostResult.fromHttpErrorResponse(errorResponse)]);
        });;
    });
  }

  public sendForgotPasswordMail(model: ForgotPasswordModel): Observable<PostResult> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/forgotpassword', model)
        .map<Object, PostResult>(() => {
          return PostResult.success();
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([PostResult.fromHttpErrorResponse(errorResponse)]);
        });;
    });
  }

  public resetPassword(model: ResetPasswordModel): Observable<PostResult> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/resetpassword', model)
        .map<Object, PostResult>(() => {
          return PostResult.success();
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([PostResult.fromHttpErrorResponse(errorResponse)]);
        });;
    });
  }
}
