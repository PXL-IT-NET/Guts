import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ICourseModel, ICourseContentsModel } from "../viewmodels/course.model"
import { GetResult } from "../util/result";

@Injectable()
export class CourseService {

  constructor(private http: HttpClient) { }

  public getCourses(): Observable<GetResult<ICourseModel[]>> {
    return this.http.get<ICourseModel[]>('api/courses')
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ICourseModel[]>(errorResponse));
        })
      );
  }

  public getCourseContentsById(courseId: number): Observable<GetResult<ICourseContentsModel>> {
    return this.http.get<ICourseContentsModel>('api/courses/' + courseId)
      .pipe(
        map(model => GetResult.success<ICourseContentsModel>(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ICourseContentsModel>(errorResponse));
        })
      );
  }
}


