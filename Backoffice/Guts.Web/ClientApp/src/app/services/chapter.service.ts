import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { IChapterDetailsModel } from "../viewmodels/chapter.model"
import { ITopicStatisticsModel, TopicStatisticsModel, ITopicSummaryModel, TopicSummaryModel } from "../viewmodels/topic.model"
import { GetResult } from "../util/Result";
import * as moment from 'moment';

@Injectable()
export class ChapterService {
  constructor(private http: HttpClient) {
  }

  public getChapterDetails(courseId: number, chapterCode: string): Observable<GetResult<IChapterDetailsModel>> {
    return this.http.get<IChapterDetailsModel>('api/courses/' + courseId + '/chapters/' + chapterCode)
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<IChapterDetailsModel>(errorResponse));
        })
      );
  }

  public getChapterSummary(courseId: number, chapterCode: string, userId: number, date?: moment.Moment): Observable<GetResult<TopicSummaryModel>> {
    var apiUrl = 'api/courses/' + courseId + '/chapters/' + chapterCode + '/users/' + userId + '/summary';
    if (date) {
      apiUrl += '?date=' + date.toISOString();
    }

    return this.http.get<ITopicSummaryModel>(apiUrl)
      .pipe(
        map(model => GetResult.success(new TopicSummaryModel(model))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<TopicSummaryModel>(errorResponse));
        })
      );
  }

  public getChapterStatistics(courseId: number, chapterCode: string, date?: moment.Moment): Observable<GetResult<TopicStatisticsModel>> {
    var apiUrl = 'api/courses/' + courseId + '/chapters/' + chapterCode + '/statistics';
    if (date) {
      apiUrl += '?date=' + date.toISOString();
    }

    return this.http.get<ITopicStatisticsModel>(apiUrl)
      .pipe(
        map((chapterStatistics: ITopicStatisticsModel) => {
          return GetResult.success(new TopicStatisticsModel(chapterStatistics));
        }),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<TopicStatisticsModel>(errorResponse));
        })
      );
  }
}
