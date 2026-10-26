import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { map, catchError } from "rxjs/operators";
import {
  ICourseModel,
  ICourseContentsModel,
  ICourseStatisticsModel,
} from "../viewmodels/course.model";
import { GetResult } from "../util/result";

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
}
