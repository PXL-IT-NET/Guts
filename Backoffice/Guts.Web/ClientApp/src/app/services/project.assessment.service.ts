import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { CreateResult, GetResult } from "../util/Result";
import { IProjectAssessmentModel, ProjectAssessmentCreateModel, ProjectAssessmentModel } from "../viewmodels/projectassessment.model";

@Injectable()
export class ProjectAssessmentService {
  constructor(private http: HttpClient) {
  }

  public getAssessmentsOfProject(projectId: number): Observable<GetResult<ProjectAssessmentModel[]>> {
    return this.http.get<IProjectAssessmentModel[]>('api/project-assessments/of-project/' + projectId)
      .pipe(
        map(assesmentModels => GetResult.success(assesmentModels.map(model => new ProjectAssessmentModel(model)))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(GetResult.fromHttpErrorResponse<ProjectAssessmentModel[]>(errorResponse));
        })
      );
  }

  public addProjectAssessment(model: ProjectAssessmentCreateModel): Observable<CreateResult<ProjectAssessmentModel>> {
    return this.http.post<IProjectAssessmentModel>('api/project-assessments', model)
      .pipe(
        map(model => CreateResult.success<ProjectAssessmentModel>(new ProjectAssessmentModel(model))),
        catchError((errorResponse: HttpErrorResponse) => {
          return of(CreateResult.fromHttpErrorResponse<ProjectAssessmentModel>(errorResponse));
        })
      );
  }
}