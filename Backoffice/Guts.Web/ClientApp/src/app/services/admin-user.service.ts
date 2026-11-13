import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse, HttpParams } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { GetResult, PostResult } from "../util/result";

export interface IAdminUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isLinkedToCourse: boolean;
  canDelete: boolean;
  canImpersonate: boolean;
  canPromoteToAdmin: boolean;
  canDemoteFromAdmin: boolean;
}

@Injectable({
  providedIn: "root",
})
export class AdminUserService {
  constructor(private readonly http: HttpClient) {}

  public searchUsers(
    search: string = "",
    role: string = "",
    OrphanedOnly: boolean = false,
  ): Observable<GetResult<IAdminUser[]>> {
    let params = new HttpParams();
    if (search.trim()) {
      params = params.set("search", search.trim());
    }
    if (role) {
      params = params.set("role", role);
    }
    if (OrphanedOnly) {
      params = params.set("OrphanedOnly", "true");
    }

    return this.http.get<IAdminUser[]>("api/admin/users", { params }).pipe(
      map((users) => GetResult.success(users)),
      catchError((errorResponse: HttpErrorResponse) =>
        of(GetResult.fromHttpErrorResponse<IAdminUser[]>(errorResponse)),
      ),
    );
  }

  public deleteUser(userId: number): Observable<PostResult> {
    return this.http.delete(`api/admin/users/${userId}`).pipe(
      map(() => PostResult.success()),
      catchError((errorResponse: HttpErrorResponse) =>
        of(PostResult.fromHttpErrorResponse(errorResponse)),
      ),
    );
  }

  public deleteAllOrphanedUsers(): Observable<GetResult<{ deletedCount: number }>> {
    return this.http
      .delete<{ deletedCount: number }>("api/admin/users/Orphaned")
      .pipe(
        map((res) => GetResult.success(res)),
        catchError((errorResponse: HttpErrorResponse) =>
          of(
            GetResult.fromHttpErrorResponse<{ deletedCount: number }>(
              errorResponse,
            ),
          ),
        ),
      );
  }

  public promoteToAdmin(userId: number): Observable<PostResult> {
    return this.http.post(`api/admin/users/${userId}/promote-admin`, {}).pipe(
      map(() => PostResult.success()),
      catchError((errorResponse: HttpErrorResponse) =>
        of(PostResult.fromHttpErrorResponse(errorResponse)),
      ),
    );
  }

  public demoteFromAdmin(userId: number): Observable<PostResult> {
    return this.http.post(`api/admin/users/${userId}/demote-admin`, {}).pipe(
      map(() => PostResult.success()),
      catchError((errorResponse: HttpErrorResponse) =>
        of(PostResult.fromHttpErrorResponse(errorResponse)),
      ),
    );
  }
}
