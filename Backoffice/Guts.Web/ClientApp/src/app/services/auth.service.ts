import { Injectable, signal } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { BehaviorSubject, Observable, of } from "rxjs";
import { map, catchError } from "rxjs/operators";
import { LoginModel } from "../viewmodels/login.model";
import { TokenModel } from "../viewmodels/token.model";
import { RegisterModel } from "../viewmodels/register.model";
import { LocalStorageKeys } from "../util/localstorage.keys";
import { PostResult } from "../util/result";
import { ConfirmEmailModel } from "../viewmodels/confirmemail.model";
import { ForgotPasswordModel } from "../viewmodels/forgotpassword.model";
import { ResetPasswordModel } from "../viewmodels/resetpassword.model";
import { ChangePasswordModel } from "../viewmodels/changepassword.model";
import { IUserProfile, UserProfile } from "../viewmodels/user.model";

@Injectable({
  providedIn: "root",
})
export class AuthService {
  private tokenModel: TokenModel | null;
  private readonly loggedInStateSubject: BehaviorSubject<boolean>;
  private readonly _isLoggedIn = signal<boolean>(false);
  public readonly isLoggedIn = this._isLoggedIn.asReadonly();
  private readonly _isImpersonating = signal<boolean>(false);
  public readonly isImpersonating = this._isImpersonating.asReadonly();
  private readonly _impersonatedProfile = signal<UserProfile | null>(null);
  public readonly impersonatedProfile = this._impersonatedProfile.asReadonly();
  private currentUserProfile: UserProfile = null;

  constructor(private readonly http: HttpClient) {
    // set token if saved in local storage
    this.tokenModel = JSON.parse(
      String(localStorage.getItem(LocalStorageKeys.currentToken)),
    );
    let isLoggedIn: boolean = Boolean(this.tokenModel?.token);
    this._isLoggedIn.set(isLoggedIn);
    this._isImpersonating.set(
      Boolean(localStorage.getItem(LocalStorageKeys.originalToken)),
    );
    this.loggedInStateSubject = new BehaviorSubject<boolean>(isLoggedIn);
  }

  private setLoggedInState(loggedIn: boolean): void {
    this._isLoggedIn.set(loggedIn);
    this.loggedInStateSubject.next(loggedIn);
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
    localStorage.removeItem(LocalStorageKeys.originalToken);

    this._isImpersonating.set(false);
    this._impersonatedProfile.set(null);
    this.setLoggedInState(false);
    this.currentUserProfile = null;
  }

  public startImpersonation(impersonationToken: TokenModel): void {
    if (this.tokenModel) {
      localStorage.setItem(
        LocalStorageKeys.originalToken,
        JSON.stringify(this.tokenModel),
      );
    }
    this.tokenModel = impersonationToken;
    localStorage.setItem(
      LocalStorageKeys.currentToken,
      JSON.stringify(impersonationToken),
    );
    this.currentUserProfile = null;
    this._isImpersonating.set(true);
    this.setLoggedInState(true);
  }

  public stopImpersonation(): void {
    const originalRaw = localStorage.getItem(LocalStorageKeys.originalToken);
    if (!originalRaw) {
      return;
    }
    const originalToken = JSON.parse(originalRaw) as TokenModel;
    this.tokenModel = originalToken;
    localStorage.setItem(
      LocalStorageKeys.currentToken,
      JSON.stringify(originalToken),
    );
    localStorage.removeItem(LocalStorageKeys.originalToken);
    this.currentUserProfile = null;
    this._isImpersonating.set(false);
    this._impersonatedProfile.set(null);
    this.setLoggedInState(true);
  }

  public invalidateUserProfileCache(): void {
    this.currentUserProfile = null;
  }

  public login(model: LoginModel): Observable<PostResult> {
    return this.http.post("api/auth/token", model).pipe(
      map((object: Object) => {
        const tokenModel = object as TokenModel;
        if (tokenModel?.token) {
          // set token property
          this.tokenModel = tokenModel;

          // store username and jwt token in local storage to keep user logged in between page refreshes
          localStorage.setItem(
            LocalStorageKeys.currentToken,
            JSON.stringify(tokenModel),
          );

          // return true to indicate successful login
          this.setLoggedInState(true);
          return PostResult.success();
        } else {
          // return false to indicate failed login
          this.setLoggedInState(false);
          const result = new PostResult();
          result.isAuthenticated = false;
          result.message = "No token present in returned token model";
          return result;
        }
      }),
      catchError((errorResponse: HttpErrorResponse) => {
        this.setLoggedInState(false);
        return of(PostResult.fromHttpErrorResponse(errorResponse));
      }),
    );
  }

  public cancelLoginSession(
    loginSessionPublicIdentifier: string,
  ): Observable<PostResult> {
    return this.http
      .patch(
        "api/auth/loginsession/" + loginSessionPublicIdentifier + "/cancel",
        null,
      )
      .pipe(map(() => PostResult.success()));
  }

  public getLoggedInState(): Observable<boolean> {
    return this.loggedInStateSubject.asObservable();
  }

  public getUserProfile(): Observable<UserProfile> {
    if (!this.currentUserProfile) {
      return this.http.get<IUserProfile>("api/users/current/profile").pipe(
        map((profile) => {
          this.currentUserProfile = new UserProfile(profile);
          if (this.currentUserProfile.isImpersonating || this._isImpersonating()) {
            this._isImpersonating.set(true);
            this._impersonatedProfile.set(this.currentUserProfile);
          }
          return this.currentUserProfile;
        }),
      );
    } else {
      return of(this.currentUserProfile);
    }
  }

  public logout(): void {
    this.clearToken();
  }

  public register(model: RegisterModel): Observable<PostResult> {
    return this.http.post("api/auth/register", model).pipe(
      map(() => {
        return PostResult.success();
      }),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(PostResult.fromHttpErrorResponse(errorResponse));
      }),
    );
  }

  public confirmEmail(model: ConfirmEmailModel): Observable<PostResult> {
    return this.http.post("api/auth/confirmemail", model).pipe(
      map(() => {
        return PostResult.success();
      }),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(PostResult.fromHttpErrorResponse(errorResponse));
      }),
    );
  }

  public sendForgotPasswordMail(
    model: ForgotPasswordModel,
  ): Observable<PostResult> {
    return this.http.post("api/auth/forgotpassword", model).pipe(
      map(() => {
        return PostResult.success();
      }),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(PostResult.fromHttpErrorResponse(errorResponse));
      }),
    );
  }

  public resetPassword(model: ResetPasswordModel): Observable<PostResult> {
    return this.http.post("api/auth/resetpassword", model).pipe(
      map(() => {
        return PostResult.success();
      }),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(PostResult.fromHttpErrorResponse(errorResponse));
      }),
    );
  }

  public changePassword(model: ChangePasswordModel): Observable<PostResult> {
    return this.http
      .post("api/auth/changepassword", {
        currentPassword: model.currentPassword,
        newPassword: model.newPassword,
      })
      .pipe(
        map(() => {
          return PostResult.success();
        }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        }),
      );
  }
}
