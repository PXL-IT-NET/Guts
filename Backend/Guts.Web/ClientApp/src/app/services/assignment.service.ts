import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IAssignmentDetailModel } from "../viewmodels/assignmentdetail.model";

@Injectable()
export class AssignmentService {

  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
  }

  public getAssignmentDetail(assignmentId: number, userId: number, date?: Date): Observable<IAssignmentDetailModel> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      let url = settings.apiBaseUrl + 'api/assignments/' + assignmentId;
      if (userId && userId > 0) {
        url += '/foruser/' + userId;
      }
      if (date) {
        url += '?date=' + date.toISOString();
      }
      return this.http.get<IAssignmentDetailModel>(url);
    });
  }

}
