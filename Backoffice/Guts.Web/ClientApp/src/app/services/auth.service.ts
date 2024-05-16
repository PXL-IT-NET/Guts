import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { LoginModel } from '../viewmodels/login.model';
import { TokenModel } from '../viewmodels/token.model';
import { RegisterModel } from '../viewmodels/register.model';
import { LocalStorageKeys } from '../util/localstorage.keys';
import { PostResult } from "../util/result";
import { ConfirmEmailModel } from '../viewmodels/confirmemail.model';
import { ForgotPasswordModel } from '../viewmodels/forgotpassword.model';
import { ResetPasswordModel } from '../viewmodels/resetpassword.model';
import { IUserProfile, UserProfile } from "../viewmodels/user.model";


@Injectable()
export class AuthService {
  private tokenModel: TokenModel | null;
  private loggedInStateSubject: BehaviorSubject<boolean>;
  private currentUserProfile: UserProfile;

  constructor(private http: HttpClient) {
    this.currentUserProfile = null;

    // set token if saved in local storage
    this.tokenModel = JSON.parse(String(localStorage.getItem(LocalStorageKeys.currentToken)));
    let isLoggedIn: boolean = Boolean(this.tokenModel && this.tokenModel.token);
    this.loggedInStateSubject = new BehaviorSubject<boolean>(isLoggedIn);
  }

  public getToken(): string | null {
    if (this.tokenModel) {
      return this.tokenModel.token;
    }
    return null;
  }

  public clearToken(): void {
    this.tokenModel = null;
    localStorage.removeItem(LocalStorageKeys.currentToken);
    this.currentUserProfile = null;
    this.loggedInStateSubject.next(false);
  }

  public invalidateUserProfileCache(): void {
    this.currentUserProfile = null;
  }

  public login(model: LoginModel): Observable<PostResult> {
    return this.http.post('api/auth/token', model)
      .pipe(
        map((object: Object) => {
          var tokenModel = object as TokenModel;
          if (tokenModel && tokenModel.token) {
            // set token property
            this.tokenModel = tokenModel;

            // store username and jwt token in local storage to keep user logged in between page refreshes
            localStorage.setItem(LocalStorageKeys.currentToken, JSON.stringify(tokenModel));

            // return true to indicate successful login
            this.loggedInStateSubject.next(true);
            return PostResult.success();
          } else {
            // return false to indicate failed login
            this.loggedInStateSubject.next(false);
            var result = new PostResult();
            result.isAuthenticated = false;
            result.message = 'No token present in returned token model';
            return result;
          }
        }),
        catchError((errorResponse: HttpErrorResponse) => {
          this.loggedInStateSubject.next(false);
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public cancelLoginSession(loginSessionPublicIdentifier: string): Observable<PostResult> {
    return this.http.patch('api/auth/loginsession/' + loginSessionPublicIdentifier + '/cancel',
      null).pipe(
        map(() => PostResult.success())
      );
  }

  public getLoggedInState(): Observable<boolean> {
    return this.loggedInStateSubject.asObservable();
  }

  public getUserProfile(): Observable<UserProfile> {
    if (!this.currentUserProfile) {
      return this.http.get<IUserProfile>('api/users/current/profile')
        .pipe(
          map(profile => {
            this.currentUserProfile = new UserProfile(profile);
            return this.currentUserProfile;
          })
        );
    } else {
      return of(this.currentUserProfile);
    }
  }

  public logout(): void {
    // clear token remove user from local storage to log user out
    this.tokenModel = null;
    localStorage.removeItem(LocalStorageKeys.currentToken);
    this.loggedInStateSubject.next(false);
    this.currentUserProfile = null;
  }

  public register(model: RegisterModel): Observable<PostResult> {
    return this.http.post('api/auth/register', model)
      .pipe(
        map(() => { return PostResult.success(); }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public confirmEmail(model: ConfirmEmailModel): Observable<PostResult> {
    return this.http.post('api/auth/confirmemail', model)
      .pipe(
        map(() => { return PostResult.success(); }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public sendForgotPasswordMail(model: ForgotPasswordModel): Observable<PostResult> {
    return this.http.post('api/auth/forgotpassword', model)
      .pipe(
        map(() => { return PostResult.success(); }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public resetPassword(model: ResetPasswordModel): Observable<PostResult> {
    return this.http.post('api/auth/resetpassword', model)
      .pipe(
        map(() => { return PostResult.success(); }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }
}
