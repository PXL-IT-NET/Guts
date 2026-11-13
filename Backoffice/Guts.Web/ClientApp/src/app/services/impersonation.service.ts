import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse, HttpParams } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { GetResult } from "../util/result";
import { TokenModel } from "../viewmodels/token.model";

export interface IImpersonatableUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

@Injectable({
  providedIn: "root",
})
export class ImpersonationService {
  constructor(private readonly http: HttpClient) {}

  public searchUsers(
    search: string = "",
  ): Observable<GetResult<IImpersonatableUser[]>> {
    let params = new HttpParams();
    if (search.trim()) {
      params = params.set("search", search.trim());
    }

    return this.http
      .get<IImpersonatableUser[]>("api/impersonation/users", { params })
      .pipe(
        map((users) => GetResult.success(users)),
        catchError((errorResponse: HttpErrorResponse) =>
          of(
            GetResult.fromHttpErrorResponse<IImpersonatableUser[]>(
              errorResponse,
            ),
          ),
        ),
      );
  }

  public impersonate(userId: number): Observable<GetResult<TokenModel>> {
    return this.http
      .post<TokenModel>(`api/impersonation/${userId}`, {})
      .pipe(
        map((token) => GetResult.success(token)),
        catchError((errorResponse: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<TokenModel>(errorResponse)),
        ),
      );
  }
}
