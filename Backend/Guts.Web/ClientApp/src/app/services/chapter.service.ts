import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IChapterDetailsModel} from "../viewmodels/chapter.model"
import { ITopicStatisticsModel, TopicStatisticsModel, ITopicSummaryModel, TopicSummaryModel  } from "../viewmodels/topic.model"
import { GetResult } from "../util/Result";
import * as moment from 'moment';

@Injectable()
export class ChapterService {
  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
  }

  public getChapterDetails(courseId: number, chapterCode: string): Observable<GetResult<IChapterDetailsModel>> {

    return this.settingsService.get().mergeMap<ClientSettings, GetResult<IChapterDetailsModel>>((settings: ClientSettings) => {
      return this.http
        .get<IChapterDetailsModel>(settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterCode)
        .map(model => GetResult.success(model))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<IChapterDetailsModel>(errorResponse)]);
        });
    });
  }

  public getChapterSummary(courseId: number, chapterCode: string, userId: number, date?: moment.Moment): Observable<GetResult<TopicSummaryModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<TopicSummaryModel>>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterCode + '/users/' + userId + '/summary';
      if (date) {
        apiUrl += '?date=' + date.toISOString();
      }

      return this.http.get<ITopicSummaryModel>(apiUrl)
        .map<ITopicSummaryModel, GetResult<TopicSummaryModel>>(model => GetResult.success(new TopicSummaryModel(model)))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<TopicSummaryModel>(errorResponse)]);
        });
    });
  }

  public getChapterStatistics(courseId: number, chapterCode: string, date?: moment.Moment): Observable<GetResult<TopicStatisticsModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<TopicStatisticsModel>>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterCode + '/statistics';
      if (date) {
        apiUrl += '?date=' + date.toISOString();
      }
      return this.http.get<ITopicStatisticsModel>(apiUrl)
        .map<ITopicStatisticsModel, GetResult<TopicStatisticsModel>>((chapterStatistics: ITopicStatisticsModel) => {
          return GetResult.success(new TopicStatisticsModel(chapterStatistics));
        })
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<TopicStatisticsModel>(errorResponse)]);
        });
    });
  }
}
