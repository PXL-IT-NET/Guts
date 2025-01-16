import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { IChapterDetailsModel } from "../viewmodels/chapter.model"
import { ITopicStatisticsModel, TopicStatisticsModel, ITopicSummaryModel, TopicSummaryModel, ITopicUpdateModel } from "../viewmodels/topic.model"
import { GetResult, PostResult } from "../util/result";
import * as moment from 'moment';
import { PeriodProvider } from './period.provider';

@Injectable()
export class ChapterService {
  constructor(
    private http: HttpClient, 
    private periodProvider: PeriodProvider) {
  }

  public getChapterDetails(courseId: number, chapterCode: string): Observable<GetResult<IChapterDetailsModel>> {
    var apiUrl = 'api/courses/' + courseId + '/chapters/' + chapterCode;
    if(this.periodProvider.period){
      apiUrl += '?periodId=' + this.periodProvider.period.id;
    }
    return this.http.get<IChapterDetailsModel>(apiUrl)
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<IChapterDetailsModel>(errorResponse));
        })
      );
  }

  public updateChapter(courseId: number, chapterCode: string, model: ITopicUpdateModel): Observable<PostResult> {
    return this.http.put('api/courses/' + courseId + '/chapters/' + chapterCode , model)
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public getChapterSummary(courseId: number, chapterCode: string, userId: number, date?: moment.Moment): Observable<GetResult<TopicSummaryModel>> {
    var apiUrl = 'api/courses/' + courseId + '/chapters/' + chapterCode + '/users/' + userId + '/summary';
    if(this.periodProvider.period){
      apiUrl += '?periodId=' + this.periodProvider.period.id;
    }
    if (date) {
      apiUrl += (this.periodProvider.period ? '&' : '?') +'date=' + date.toISOString();
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
    if(this.periodProvider.period){
      apiUrl += '?periodId=' + this.periodProvider.period.id;
    }
    if (date) {
      apiUrl += (this.periodProvider.period ? '&' : '?') +'date=' + date.toISOString();
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
