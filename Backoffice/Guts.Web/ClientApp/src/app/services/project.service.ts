import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { IProjectDetailsModel } from "../viewmodels/project.model";
import { ITeamDetailsModel } from "../viewmodels/team.model"
import { CreateResult, GetResult, PostResult } from "../util/result";
import * as moment from 'moment';
import { ITopicStatisticsModel, TopicStatisticsModel, ITopicSummaryModel, TopicSummaryModel, ITopicUpdateModel, ITopicAddModel } from "../viewmodels/topic.model"
import { TeamGenerationModel } from "../viewmodels/team.model"

@Injectable()
export class ProjectService {
  constructor(private http: HttpClient) {
  }

  public getProjectDetails(courseId: number, projectCode: string): Observable<GetResult<IProjectDetailsModel>> {
    return this.http.get<IProjectDetailsModel>('api/courses/' + courseId + '/projects/' + projectCode)
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<IProjectDetailsModel>(errorResponse));
        })
      );
  }

  public addProject(courseId: number, model: ITopicAddModel): Observable<CreateResult<IProjectDetailsModel>> {
    return this.http.post('api/courses/' + courseId + '/projects' , model)
      .pipe(
        map((addedProject: IProjectDetailsModel) => CreateResult.success(addedProject)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(CreateResult.fromHttpErrorResponse<IProjectDetailsModel>(errorResponse));
        })
      );
  }

  public updateProject(courseId: number, projectCode: string, model: ITopicUpdateModel): Observable<PostResult> {
    return this.http.put('api/courses/' + courseId + '/projects/' + projectCode , model)
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public getTeams(courseId: number, projectCode: string): Observable<GetResult<ITeamDetailsModel[]>> {
    return this.http.get<ITeamDetailsModel[]>('api/courses/' + courseId + '/projects/' + projectCode + '/teams')
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ITeamDetailsModel[]>(errorResponse));
        })
      );
  }

  public getMyTeam(courseId: number, projectCode: string): Observable<GetResult<ITeamDetailsModel>> {
    return this.http.get<ITeamDetailsModel>('api/courses/' + courseId + '/projects/' + projectCode + '/teams/my-team')
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ITeamDetailsModel>(errorResponse));
        })
      );
  }

  public updateTeam(courseId: number, projectCode: string, team: ITeamDetailsModel): Observable<PostResult> {
    return this.http.put('api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + team.id, team)
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public addTeam(courseId: number, projectCode: string, teamName: string): Observable<CreateResult<ITeamDetailsModel>> {
    return this.http.post<ITeamDetailsModel>('api/courses/' + courseId + '/projects/' + projectCode + '/teams', { 'name': teamName })
      .pipe(
        map((addedTeam: ITeamDetailsModel) => CreateResult.success(addedTeam)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(CreateResult.fromHttpErrorResponse<ITeamDetailsModel>(errorResponse));
        })
      );
  }

  public generateTeams(courseId: number, projectCode: string, model: TeamGenerationModel) {
    return this.http.post('api/courses/' + courseId + '/projects/' + projectCode + '/teams/generate', model)
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public deleteTeam(courseId: number, projectCode: string, teamId: number): Observable<PostResult> {
    return this.http.delete('api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + teamId, {})
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public joinTeam(courseId: number, projectCode: string, teamId: number): Observable<PostResult> {
    return this.http.post('api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + teamId + '/join', {})
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public leaveTeam(courseId: number, projectCode: string, teamId: number): Observable<PostResult> {
    return this.http.post('api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + teamId + '/leave', {})
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public removeFromTeam(courseId: number, projectCode: string, teamId: number, userId: number): Observable<PostResult> {
    return this.http.post('api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + teamId + '/remove', userId)
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }

  public getProjectSummary(courseId: number, projectCode: string, teamId: number, date?: moment.Moment): Observable<GetResult<TopicSummaryModel>> {
    var apiUrl = 'api/courses/' + courseId + '/projects/' + projectCode + '/teams/' + teamId + '/summary';
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

  public getProjectStatistics(courseId: number, projectCode: string, date?: moment.Moment): Observable<GetResult<TopicStatisticsModel>> {
    var apiUrl = 'api/courses/' + courseId + '/projects/' + projectCode + '/statistics';
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
