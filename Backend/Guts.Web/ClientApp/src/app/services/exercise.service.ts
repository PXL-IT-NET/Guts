import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IExerciseDetailModel } from "../viewmodels/exercisedetail.model";
import { IUserModel } from "../viewmodels/user.model";

@Injectable()
export class ExerciseService {

  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
  }

  public getExerciseDetail(exerciseId: number, userId: number, date?: Date): Observable<IExerciseDetailModel> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      let url = settings.apiBaseUrl + 'api/exercises/' + exerciseId;
      if (userId && userId > 0) {
        url += '/foruser/' + userId;
      }
      if (date) {
        url += '?date=' + date.toISOString();
      }
      return this.http.get<IExerciseDetailModel>(url);
    });
  }

}
