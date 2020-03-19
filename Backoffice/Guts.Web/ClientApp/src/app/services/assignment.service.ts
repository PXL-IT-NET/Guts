import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { IAssignmentDetailModel } from "../viewmodels/assignmentdetail.model";
import * as moment from 'moment';
import { ITopicAssignmentModel } from '../viewmodels/assignment.model';
import { GetResult } from '../util/Result';
import { map, catchError } from 'rxjs/operators';

@Injectable()
export class AssignmentService {

  constructor(private http: HttpClient) {
  }

  public getAssignmentDetail(assignmentId: number, userId: number, teamId: number, date?: moment.Moment): Observable<IAssignmentDetailModel> {
    let url = 'api/assignments/' + assignmentId;
    if (userId && userId > 0) {
      url += '/foruser/' + userId;
    } else if (teamId && teamId > 0) {
      url += '/forteam/' + teamId;
    }
    if (date) {
      url += '?date=' + date.toISOString();
    }
    return this.http.get<IAssignmentDetailModel>(url);
  }

  public getAssignmentsOfCourse(courseId: number): Observable<GetResult<ITopicAssignmentModel[]>> {
    return this.http.get<ITopicAssignmentModel[]>('api/assignments/ofcourse/' + courseId)
    .pipe(
      map((assignments: ITopicAssignmentModel[]) => {
        return GetResult.success(assignments);
      }),
      catchError((errorResponse: HttpErrorResponse) => {
        return of(GetResult.fromHttpErrorResponse<ITopicAssignmentModel[]>(errorResponse));
      })
    );
  }

}
