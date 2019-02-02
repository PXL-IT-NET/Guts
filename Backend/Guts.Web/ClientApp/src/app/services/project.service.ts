import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IProjectDetailsModel } from "../viewmodels/project.model";
import { ITeamDetailsModel } from "../viewmodels/team.model"
import { GetResult, PostResult } from "../util/Result";
import * as moment from 'moment';
import { ITopicStatisticsModel, TopicStatisticsModel, ITopicSummaryModel, TopicSummaryModel } from "../viewmodels/topic.model"

@Injectable()
export class ProjectService {
  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
  }

  public getProjectDetails(courseId: number, projectCode: string): Observable<GetResult<IProjectDetailsModel>> {

    return this.settingsService.get().mergeMap<ClientSettings, GetResult<IProjectDetailsModel>>((settings: ClientSettings) => {
      return this.http
        .get<IProjectDetailsModel>(settings.apiBaseUrl + 'api/courses/' + courseId + '/projects/' + projectCode)
        .map(model => GetResult.success(model))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<IProjectDetailsModel>(errorResponse)]);
        });
    });
  }

  public getTeams(courseId: number, projectCode: string): Observable<GetResult<ITeamDetailsModel[]>>{
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<ITeamDetailsModel[]>>((settings: ClientSettings) => {
      return this.http
        .get<ITeamDetailsModel[]>(settings.apiBaseUrl + 'api/courses/' + courseId + '/projects/' + projectCode + '/teams')
        .map(model => GetResult.success(model))
        .catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([GetResult.fromHttpErrorResponse<ITeamDetailsModel[]>(errorResponse)]);
        });
    });
  }

  public joinTeam(courseId: number, projectCode: string, teamId: number): Observable<PostResult> {
    return this.settingsService.get().mergeMap<ClientSettings, PostResult>((settings: ClientSettings) => {
      return this.http
        .post(settings.apiBaseUrl + 'api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + teamId, {})
        .map<Object, PostResult>(() => {
          return PostResult.success();
        }).catch((errorResponse: HttpErrorResponse) => {
          return Observable.from([PostResult.fromHttpErrorResponse(errorResponse)]);
        });
    });
  }

  public getProjectSummary(courseId: number, projectCode: string, teamId: number, date?: moment.Moment): Observable<GetResult<TopicSummaryModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<TopicSummaryModel>>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + teamId + '/summary';
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

  public getProjectStatistics(courseId: number, projectCode: string, date?: moment.Moment): Observable<GetResult<TopicStatisticsModel>> {
    return this.settingsService.get().mergeMap<ClientSettings, GetResult<TopicStatisticsModel>>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/projects/' + projectCode + '/statistics';
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
