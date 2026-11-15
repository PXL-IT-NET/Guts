import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { GetResult, PostResult } from "../util/result";
import { createPagedSearchParams, IPagedResult } from "../util/paged-result";

export type { IPagedResult } from "../util/paged-result";

export interface ILinkedCourse {
  id: number;
  code: string;
  name: string;
}

export interface IAdminUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  isLinkedToCourse: boolean;
  linkedCourses: ILinkedCourse[];
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
    orphanedOnly: boolean = false,
    page: number = 1,
    pageSize: number | null = 25,
  ): Observable<GetResult<IPagedResult<IAdminUser>>> {
    let params = createPagedSearchParams(search, page, pageSize);
    if (role) {
      params = params.set("role", role);
    }
    if (orphanedOnly) {
      params = params.set("orphanedOnly", "true");
    }

    return this.http
      .get<IPagedResult<IAdminUser>>("api/admin/users", { params })
      .pipe(
        map((result) => GetResult.success(result)),
        catchError((errorResponse: HttpErrorResponse) =>
          of(
            GetResult.fromHttpErrorResponse<IPagedResult<IAdminUser>>(
              errorResponse,
            ),
          ),
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

  public deleteUsers(
    userIds: number[],
  ): Observable<GetResult<{ deletedCount: number }>> {
    return this.http
      .post<{ deletedCount: number }>("api/admin/users/delete-batch", {
        userIds,
      })
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
