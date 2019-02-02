import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IProjectDetailsModel } from "../viewmodels/project.model";
import { ITeamDetailsModel } from "../viewmodels/team.model"
import { GetResult, PostResult } from "../util/Result";

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
}
