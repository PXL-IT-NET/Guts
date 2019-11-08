import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { GetResult, CreateResult } from "../util/Result";
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { IExamModel, ExamModel } from "../viewmodels/exam.model"


@Injectable()
export class ExamService {

  constructor(private http: HttpClient) {
  }

  public getExamsOfCourse(courseId: number): Observable<GetResult<ExamModel[]>> {
    return this.http.get<IExamModel[]>('api/exams?courseId=' + courseId)
      .pipe(
        map(examModels => GetResult.success(examModels.map(model => new ExamModel(model)))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ExamModel[]>(errorResponse));
        })
      );
  }

  public saveExam(model: IExamModel): Observable<CreateResult<ExamModel>> {
    return this.http.post<IExamModel>('api/exams', model)
      .pipe(
        map((savedExam: IExamModel) => {
          return CreateResult.success(new ExamModel(savedExam));
        }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(CreateResult.fromHttpErrorResponse<ExamModel>(errorResponse));
        })
      );
  }
}
