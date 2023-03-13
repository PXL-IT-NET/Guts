import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { CreateResult, GetResult } from "../util/Result";
import { IProjectAssessmentModel, IProjectTeamAssessmentStatusModel, ProjectAssessmentCreateModel, ProjectAssessmentModel, ProjectTeamAssessmentStatusModel } from "../viewmodels/projectassessment.model";

@Injectable()
export class ProjectTeamAssessmentService {
  constructor(private http: HttpClient) {
  }

  public getStatusOfProjectTeamAssessment(projectAssessmentId: number, teamId: number): Observable<GetResult<ProjectTeamAssessmentStatusModel>> {
    let url = 'api/project-team-assessments/of-project-assessment/' + projectAssessmentId + '/of-team/' + teamId;
    return this.http.get<IProjectTeamAssessmentStatusModel>(url)
      .pipe(
        map(model => GetResult.success(new ProjectTeamAssessmentStatusModel(model))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ProjectTeamAssessmentStatusModel>(errorResponse));
        })
      );
  }
}