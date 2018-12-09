import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap'
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { ICourseModel, ICourseContentsModel } from "../viewmodels/course.model"
import { GetResult } from "../util/Result";

@Injectable()
export class CourseService {
  private apiBaseUrl: string;

  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
    this.apiBaseUrl = '';
  }

  public getCourses(): Observable<GetResult<ICourseModel[]>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<ICourseModel[]>>((settings: ClientSettings) => {
      return this.http.get<ICourseModel[]>(settings.apiBaseUrl + 'api/courses')
        .map(model => GetResult.success(model))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }

  public getCourseContentsById(courseId: number): Observable<GetResult<ICourseContentsModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<ICourseContentsModel>>((settings: ClientSettings) => {
      return this.http.get<ICourseContentsModel>(settings.apiBaseUrl + 'api/courses/' + courseId)
        .map(model => GetResult.success(model))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }
}
