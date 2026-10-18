import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { IProjectDetailsModel } from "../viewmodels/project.model";
import { ITeamDetailsModel, TeamGenerationModel } from "../viewmodels/team.model";
import { CreateResult, GetResult, PostResult } from "../util/result";
import {
  TopicStatisticsModel,
  TopicSummaryModel,
  ITopicUpdateModel,
  ITopicAddModel,
} from "../viewmodels/topic.model";
import { PeriodProvider } from "./period.provider";
import { TopicServiceBase } from "./topic.service.base";

@Injectable({
  providedIn: "root",
})
export class ProjectService extends TopicServiceBase {
  constructor(http: HttpClient, periodProvider: PeriodProvider) {
    super(http, periodProvider, "projects");
  }

  public getProjectDetails(
    courseId: number,
    projectCode: string,
  ): Observable<GetResult<IProjectDetailsModel>> {
    return this.getTopicDetails<IProjectDetailsModel>(courseId, projectCode);
  }

  public addProject(
    courseId: number,
    model: ITopicAddModel,
  ): Observable<CreateResult<IProjectDetailsModel>> {
    return this.postCreate<IProjectDetailsModel>(
      this.getCourseTopicsUrl(courseId),
      model,
    );
  }

  public updateProject(
    courseId: number,
    projectCode: string,
    model: ITopicUpdateModel,
  ): Observable<PostResult> {
    return this.updateTopic(courseId, projectCode, model);
  }

  public getTeams(
    courseId: number,
    projectCode: string,
  ): Observable<GetResult<ITeamDetailsModel[]>> {
    const apiUrl = this.appendPeriodAndDateQuery(
      this.getTeamsUrl(courseId, projectCode),
    );
    return this.get<ITeamDetailsModel[]>(apiUrl);
  }

  public getMyTeam(
    courseId: number,
    projectCode: string,
  ): Observable<GetResult<ITeamDetailsModel>> {
    return this.get<ITeamDetailsModel>(
      this.getTeamsUrl(courseId, projectCode, "/my-team"),
    );
  }

  public updateTeam(
    courseId: number,
    projectCode: string,
    team: ITeamDetailsModel,
  ): Observable<PostResult> {
    return this.putAction(
      this.getTeamsUrl(courseId, projectCode, `/${team.id}`),
      team,
    );
  }

  public addTeam(
    courseId: number,
    projectCode: string,
    teamName: string,
  ): Observable<CreateResult<ITeamDetailsModel>> {
    return this.postCreate<ITeamDetailsModel>(
      this.getTeamsUrl(courseId, projectCode),
      { name: teamName },
    );
  }

  public generateTeams(
    courseId: number,
    projectCode: string,
    model: TeamGenerationModel,
  ): Observable<PostResult> {
    return this.postAction(
      this.getTeamsUrl(courseId, projectCode, "/generate"),
      model,
    );
  }

  public deleteTeam(
    courseId: number,
    projectCode: string,
    teamId: number,
  ): Observable<PostResult> {
    return this.deleteAction(
      this.getTeamsUrl(courseId, projectCode, `/${teamId}`),
    );
  }

  public joinTeam(
    courseId: number,
    projectCode: string,
    teamId: number,
  ): Observable<PostResult> {
    return this.postAction(
      this.getTeamsUrl(courseId, projectCode, `/${teamId}/join`),
    );
  }

  public leaveTeam(
    courseId: number,
    projectCode: string,
    teamId: number,
  ): Observable<PostResult> {
    return this.postAction(
      this.getTeamsUrl(courseId, projectCode, `/${teamId}/leave`),
    );
  }

  public removeFromTeam(
    courseId: number,
    projectCode: string,
    teamId: number,
    userId: number,
  ): Observable<PostResult> {
    return this.postAction(
      this.getTeamsUrl(courseId, projectCode, `/${teamId}/remove`),
      userId,
    );
  }

  public getProjectSummary(
    courseId: number,
    projectCode: string,
    teamId: number,
    date?: Date | string,
  ): Observable<GetResult<TopicSummaryModel>> {
    return this.getTopicSummaryByTarget(
      courseId,
      projectCode,
      "teams",
      teamId,
      date,
    );
  }

  public getProjectStatistics(
    courseId: number,
    projectCode: string,
    date?: Date | string,
  ): Observable<GetResult<TopicStatisticsModel>> {
    return this.getTopicStatistics(courseId, projectCode, date);
  }

  private getTeamsUrl(
    courseId: number,
    projectCode: string,
    suffix = "",
  ): string {
    return this.getTopicUrl(courseId, projectCode, `/teams${suffix}`);
  }
}
