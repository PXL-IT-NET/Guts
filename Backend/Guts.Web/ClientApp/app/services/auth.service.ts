import { Injectable } from '@angular/core';
import { Http, Response } from '@angular/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/map'
import { LoginModel } from '../models/login.model';
import { TokenModel } from '../models/token.model';
import { RegisterModel } from '../models/register.model';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { LocalStorageService } from 'angular-2-local-storage';
import { LocalStorageKeys } from '../util/localstorage.keys';
import { Result } from '../util/result';
import { ConfirmEmailModel } from '../models/confirmemail.model';
import { ForgotPasswordModel } from '../models/forgotpassword.model';
import { ResetPasswordModel } from '../models/resetpassword.model';

@Injectable()
export class AuthService {
    private apiBaseUrl: string;

    public tokenModel: TokenModel | null;

    constructor(private http: Http,
        private settingsService: ClientSettingsService,
        private localStorageService: LocalStorageService) {
        // set token if saved in local storage
        var currentToken = JSON.parse(String(this.localStorageService.get(LocalStorageKeys.currentToken)));
        console.log(currentToken);
        this.tokenModel = currentToken && currentToken.token;
    }

    public login(model: LoginModel): Observable<Result> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.post(settings.apiBaseUrl + 'api/auth/token', model)
                .map((response: Response) => {
                    // login successful if there's a jwt token in the response
                    let returnedTokenModel: TokenModel = response.json();
                    if (returnedTokenModel && returnedTokenModel.token) {
                        // set token property
                        this.tokenModel = returnedTokenModel;

                        // store username and jwt token in local storage to keep user logged in between page refreshes
                        this.localStorageService.set(LocalStorageKeys.currentToken, JSON.stringify(returnedTokenModel));

                        // return true to indicate successful login
                        return Result.fromHttpResponse(response);
                    } else {
                        // return false to indicate failed login
                        return {
                            success: false,
                            message: 'No token present in returned token model'
                        };
                    }
                }).catch((errorResponse: Response) => {
                    return Observable.from([Result.fromHttpResponse(errorResponse)]);
                });;
        });
    }

    public logout(): void {
        // clear token remove user from local storage to log user out
        this.tokenModel = null;
        this.localStorageService.remove(LocalStorageKeys.currentToken);
    }

    public register(model: RegisterModel): Observable<Result> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.post(settings.apiBaseUrl + 'api/auth/register', model)
                .map((response: Response) => {
                    return Result.fromHttpResponse(response);
                })
                .catch((errorResponse: Response) => {
                    return Observable.from([Result.fromHttpResponse(errorResponse)]);
                });
        });
    }

    public confirmEmail(model: ConfirmEmailModel): Observable<Result> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.post(settings.apiBaseUrl + 'api/auth/confirmemail', model)
                .map((response: Response) => {
                    return Result.fromHttpResponse(response);
                }).catch((errorResponse: Response) => {
                    return Observable.from([Result.fromHttpResponse(errorResponse)]);
                });;
        });
    }

    public sendForgotPasswordMail(model: ForgotPasswordModel): Observable<Result> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.post(settings.apiBaseUrl + 'api/auth/forgotpassword', model)
                .map((response: Response) => {
                    return Result.fromHttpResponse(response);
                }).catch((errorResponse: Response) => {
                    return Observable.from([Result.fromHttpResponse(errorResponse)]);
                });;
        });
    }

    public resetPassword(model: ResetPasswordModel): Observable<Result> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.post(settings.apiBaseUrl + 'api/auth/resetpassword', model)
                .map((response: Response) => {
                    return Result.fromHttpResponse(response);
                }).catch((errorResponse: Response) => {
                    return Observable.from([Result.fromHttpResponse(errorResponse)]);
                });;
        });
    }
}