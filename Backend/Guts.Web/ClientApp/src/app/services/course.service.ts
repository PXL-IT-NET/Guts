import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap'
import { ICourseModel, ICourseContentsModel } from "../viewmodels/course.model"
import { GetResult } from "../util/Result";

@Injectable()
export class CourseService {

  constructor(private http: HttpClient) {}

  public getCourses(): Observable<GetResult<ICourseModel[]>> {
    return this.http.get<ICourseModel[]>('api/courses')
      .map(model => GetResult.success(model))
      .catch((errorResponse: HttpErrorResponse) => {
        return Observable.from([GetResult.fromHttpErrorResponse<ICourseModel[]>(errorResponse)]);
      });
  }

  public getCourseContentsById(courseId: number): Observable<GetResult<ICourseContentsModel>> {
    return this.http.get<ICourseContentsModel>('api/courses/' + courseId)
      .map(model => GetResult.success<ICourseContentsModel>(model))
      .catch((errorResponse: HttpErrorResponse) => {
        return Observable.from([GetResult.fromHttpErrorResponse<ICourseContentsModel>(errorResponse)]);
      });
  }
}
