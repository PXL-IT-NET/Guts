import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IChapterSummaryModel, ChapterSummaryModel, IChapterDetailsModel, IChapterStatisticsModel, ChapterStatisticsModel } from "../viewmodels/chapter.model"
import { GetResult } from "../util/Result";

@Injectable()
export class ChapterService {
  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
  }

  public getChapterDetails(courseId: number, chapterNumber: number): Observable<GetResult<IChapterDetailsModel>> {

    return this.settingsService.get().mergeMap<ClientSettings, GetResult<IChapterDetailsModel>>((settings: ClientSettings) => {
      return this.http
        .get<IChapterDetailsModel>(settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber)
        .map(model => GetResult.success(model))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<IChapterDetailsModel>(errorResponse)]);
        });
    });
  }

  public getChapterSummary(courseId: number, chapterNumber: number, userId: number, date?: Date): Observable<GetResult<ChapterSummaryModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<ChapterSummaryModel>>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber + '/users/' + userId + '/summary';
      if (date) {
        apiUrl += '?date=' + date.toISOString();
      }

      return this.http.get<IChapterSummaryModel>(apiUrl)
        .map<IChapterSummaryModel, GetResult<ChapterSummaryModel>>(model => GetResult.success(new ChapterSummaryModel(model)))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<ChapterSummaryModel>(errorResponse)]);
        });
    });
  }

  public getChapterStatistics(courseId: number, chapterNumber: number, date?: Date): Observable<GetResult<ChapterStatisticsModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<ChapterStatisticsModel>>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber + '/statistics';
      if (date) {
        apiUrl += '?date=' + date.toISOString();
      }
      return this.http.get<IChapterStatisticsModel>(apiUrl)
        .map<IChapterStatisticsModel, GetResult<ChapterStatisticsModel>>((chapterStatistics: IChapterStatisticsModel) => {
          return GetResult.success(new ChapterStatisticsModel(chapterStatistics));
        })
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<ChapterStatisticsModel>(errorResponse)]);
        });
    });
  }
}
