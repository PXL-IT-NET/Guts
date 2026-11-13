import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { map, catchError } from "rxjs/operators";
import {
  ICourseModel,
  ICourseContentsModel,
  ICourseStatisticsModel,
} from "../viewmodels/course.model";
import { GetResult, PostResult } from "../util/result";

export interface ISaveCourseRequest {
  code: string;
  name: string;
}

@Injectable({
  providedIn: "root",
})
export class CourseService {
  constructor(private readonly http: HttpClient) {}

  public getCourses(): Observable<GetResult<ICourseModel[]>> {
    return this.http.get<ICourseModel[]>("api/courses").pipe(
      map((model) => GetResult.success(model)),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(
          GetResult.fromHttpErrorResponse<ICourseModel[]>(errorResponse),
        );
      }),
    );
  }

  public getCourseContentsById(
    courseId: number,
    periodId: number = 0,
  ): Observable<GetResult<ICourseContentsModel>> {
    let url = "api/courses/" + courseId;
    if (periodId > 0) {
      url += "?periodId=" + periodId;
    }
    return this.http.get<ICourseContentsModel>(url).pipe(
      map((model) => GetResult.success<ICourseContentsModel>(model)),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(
          GetResult.fromHttpErrorResponse<ICourseContentsModel>(errorResponse),
        );
      }),
    );
  }

  public getCourseStatistics(
    courseId: number,
    periodId: number = 0,
  ): Observable<GetResult<ICourseStatisticsModel>> {
    let url = `api/courses/${courseId}/statistics`;
    if (periodId > 0) {
      url += `?periodId=${periodId}`;
    }
    return this.http.get<ICourseStatisticsModel>(url).pipe(
      map((model) => GetResult.success<ICourseStatisticsModel>(model)),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(
          GetResult.fromHttpErrorResponse<ICourseStatisticsModel>(errorResponse),
        );
      }),
    );
  }

  public createCourse(
    model: ISaveCourseRequest,
  ): Observable<GetResult<ICourseModel>> {
    return this.http.post<ICourseModel>("api/courses", model).pipe(
      map((created) => GetResult.success(created)),
      catchError((errorResponse: HttpErrorResponse) =>
        of(GetResult.fromHttpErrorResponse<ICourseModel>(errorResponse)),
      ),
    );
  }

  public updateCourse(
    courseId: number,
    model: ISaveCourseRequest,
  ): Observable<GetResult<ICourseModel>> {
    return this.http.put<ICourseModel>(`api/courses/${courseId}`, model).pipe(
      map((updated) => GetResult.success(updated)),
      catchError((errorResponse: HttpErrorResponse) =>
        of(GetResult.fromHttpErrorResponse<ICourseModel>(errorResponse)),
      ),
    );
  }

  public deleteCourse(courseId: number): Observable<PostResult> {
    return this.http.delete(`api/courses/${courseId}`).pipe(
      map(() => PostResult.success()),
      catchError((errorResponse: HttpErrorResponse) =>
        of(PostResult.fromHttpErrorResponse(errorResponse)),
      ),
    );
  }
}
