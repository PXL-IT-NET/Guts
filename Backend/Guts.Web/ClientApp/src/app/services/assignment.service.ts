import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IAssignmentDetailModel } from "../viewmodels/assignmentdetail.model";
import * as moment from 'moment';

@Injectable()
export class AssignmentService {

  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
  }

  public getAssignmentDetail(assignmentId: number, userId: number, teamId: number, date?: moment.Moment): Observable<IAssignmentDetailModel> {
    return this.settingsService.get().mergeMap<ClientSettings, IAssignmentDetailModel>((settings: ClientSettings) => {
      let url = settings.apiBaseUrl + 'api/assignments/' + assignmentId;
      if (userId && userId > 0) {
        url += '/foruser/' + userId;
      } else if (teamId && teamId > 0) {
        url += '/forteam/' + teamId;
      }
      if (date) {
        url += '?date=' + date.toISOString();
      }
      return this.http.get<IAssignmentDetailModel>(url);
    });
  }

}
