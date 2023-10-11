import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { GetResult, CreateResult, Result, PostResult } from "../util/result";
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { IExamModel, ExamModel, IExamPartModel, ExamPartModel } from "../viewmodels/exam.model";
import { saveAs } from 'file-saver';


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

  public saveExamPart(examId: number, examPart: ExamPartModel): Observable<CreateResult<ExamPartModel>> {
    let model: IExamPartModel = {
      id: examPart.id,
      name: examPart.name,
      deadline: examPart.deadline,
      assignmentEvaluations: examPart.assignmentEvaluations
    };
    return this.http.post<IExamPartModel>('api/exams/' + examId + '/parts', model)
      .pipe(
        map((savedExampart: IExamPartModel) => {
          return CreateResult.success(new ExamPartModel(savedExampart));
        }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(CreateResult.fromHttpErrorResponse<ExamPartModel>(errorResponse));
        })
      );
  }

  public deleteExamPart(examId: number, examPartId: number): Observable<Result> {
    return this.http.delete('api/exams/' + examId + '/parts/' + examPartId)
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public getExamResultsDownloadUrl(examId: number): Observable<Result> {

    return this.http.get<any>('api/exams/' + examId + '/downloadscores',
      { headers: { 'Accept': 'application/octet-stream' }, responseType: 'blob' as 'json', observe: 'response'}) //
      .pipe(
        map((resonse: HttpResponse<any>) => {
          var contentDisposition = resonse.headers.get('content-disposition');
          var filename = contentDisposition.split(';')[1].split('filename')[1].split('=')[1].trim();
          saveAs(resonse.body, filename);
          var result = new Result();
          result.success = true;
          return result;
        }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(Result.fromHttpErrorResponse(errorResponse));
        })
      );
  }
}
