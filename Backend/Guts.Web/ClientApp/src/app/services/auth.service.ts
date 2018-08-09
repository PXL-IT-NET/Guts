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
import { Result } from '../util/result';
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

  public login(model: LoginModel): Observable<Result> {

    return this.settingsService.get().mergeMap((settings: ClientSettings) => {

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
            return Result.success();
          } else {
            // return false to indicate failed login
            this.loggedInState.next(false);
            return {
              success: false,
              message: 'No token present in returned token model'
            };
          }
        }).catch((errorResponse: HttpErrorResponse) => {
          this.loggedInState.next(false);
          return Observable.from([Result.fromHttpErrorResponse(errorResponse)]);
        });
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

  public register(model: RegisterModel): Observable<Result> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/register', model)
        .map(() => {
          return Result.success();
        })
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([Result.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }

  public confirmEmail(model: ConfirmEmailModel): Observable<Result> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/confirmemail', model)
        .map(() => {
          return Result.success();
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([Result.fromHttpErrorResponse(errorResponse)]);
        });;
    });
  }

  public sendForgotPasswordMail(model: ForgotPasswordModel): Observable<Result> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/forgotpassword', model)
        .map(() => {
          return Result.success();
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([Result.fromHttpErrorResponse(errorResponse)]);
        });;
    });
  }

  public resetPassword(model: ResetPasswordModel): Observable<Result> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.post(settings.apiBaseUrl + 'api/auth/resetpassword', model)
        .map(() => {
          return Result.success();
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([Result.fromHttpErrorResponse(errorResponse)]);
        });;
    });
  }
}
