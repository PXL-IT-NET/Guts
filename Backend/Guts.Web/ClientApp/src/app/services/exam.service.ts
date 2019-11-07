import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { GetResult, CreateResult } from "../util/Result";
import { Observable } from 'rxjs';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IExamModel, ExamModel } from "../viewmodels/exam.model"
import { mergeMap } from 'rxjs/operator/mergeMap';
import { map } from 'rxjs/operator/map';


@Injectable()
export class ExamService {
  private apiBaseUrl: string;

  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
    this.apiBaseUrl = '';
  }

  public getExamsOfCourse(courseId: number): Observable<GetResult<ExamModel[]>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<ExamModel[]>>((settings: ClientSettings) => {
      return this.http.get<IExamModel[]>(settings.apiBaseUrl + 'api/exams?courseId=' + courseId)
        .map(examModels => GetResult.success(examModels.map(model => new ExamModel(model))))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }

  public saveExam(model: IExamModel): Observable<CreateResult<ExamModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, CreateResult<ExamModel>>((settings: ClientSettings) => {
      return this.http.post<IExamModel>(settings.apiBaseUrl + 'api/exams', model)
        .map<IExamModel, CreateResult<ExamModel>>((savedExam: IExamModel) => {
          return CreateResult.success(new ExamModel(savedExam));
          
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([CreateResult.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }

}
