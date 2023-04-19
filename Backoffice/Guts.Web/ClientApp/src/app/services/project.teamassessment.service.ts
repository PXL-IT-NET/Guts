import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { CreateResult, GetResult, PostResult } from "../util/Result";
import { IAssessmentResultModel, IPeerAssessmentModel, IProjectTeamAssessmentStatusModel, PeerAssessmentModel, ProjectTeamAssessmentStatusModel } from "../viewmodels/projectassessment.model";

@Injectable()
export class ProjectTeamAssessmentService {
  constructor(private http: HttpClient) {
  }

  public getStatusOfProjectTeamAssessment(projectAssessmentId: number, teamId: number): Observable<GetResult<ProjectTeamAssessmentStatusModel>> {
    let url = 'api/project-team-assessments/of-project-assessment/' + projectAssessmentId + '/of-team/' + teamId + '/status';
    return this.http.get<IProjectTeamAssessmentStatusModel>(url)
      .pipe(
        map(model => GetResult.success(new ProjectTeamAssessmentStatusModel(model))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ProjectTeamAssessmentStatusModel>(errorResponse));
        })
      );
  }

  public getDetailedResultsOfProjectTeamAssessment(projectAssessmentId: number, teamId: number): Observable<GetResult<IAssessmentResultModel[]>> {
    let url = 'api/project-team-assessments/of-project-assessment/' + projectAssessmentId + '/of-team/' + teamId + '/detailed-results';
    return this.http.get<IAssessmentResultModel[]>(url)
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<IAssessmentResultModel[]>(errorResponse));
        })
      );
  }

  public getMyResultOfProjectTeamAssessment(projectAssessmentId: number, teamId: number): Observable<GetResult<IAssessmentResultModel>> {
    let url = 'api/project-team-assessments/of-project-assessment/' + projectAssessmentId + '/of-team/' + teamId + '/my-result';
    return this.http.get<IAssessmentResultModel>(url)
      .pipe(
        map(model => GetResult.success(model)),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<IAssessmentResultModel>(errorResponse));
        })
      );
  }

  public getPeerAssessmentsOfUser(projectAssessmentId: number, teamId: number): Observable<GetResult<PeerAssessmentModel[]>> {
    let url = 'api/project-team-assessments/of-project-assessment/' + projectAssessmentId + '/of-team/' + teamId + '/peer-assessments';
    return this.http.get<IPeerAssessmentModel[]>(url)
      .pipe(
        map(models => GetResult.success(models.map(model => new PeerAssessmentModel(model)))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<PeerAssessmentModel[]>(errorResponse));
        })
      );
  }

  public savePeerAssessment(projectAssessmentId: number, teamId: number, peerAssessments: PeerAssessmentModel[]): Observable<PostResult> {
    let url = 'api/project-team-assessments/of-project-assessment/' + projectAssessmentId + '/of-team/' + teamId + '/peer-assessments';
    return this.http.post(url, peerAssessments)
      .pipe(
        map(() => PostResult.success()),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(PostResult.fromHttpErrorResponse(errorResponse));
        })
      );
  }
}