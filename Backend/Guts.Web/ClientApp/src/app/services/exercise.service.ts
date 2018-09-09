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

  public getExerciseDetail(exerciseId: number, userId?: number): Observable<IExerciseDetailModel> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      let url = settings.apiBaseUrl + 'api/exercises/' + exerciseId;
      if (userId && userId > 0) {
        url += '/foruser/' + userId;
      }
      return this.http.get<IExerciseDetailModel>(url);
    });
  }

  public getExerciseStudents(exerciseId: number): Observable<IUserModel[]> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.get<IUserModel[]>(settings.apiBaseUrl + 'api/exercises/' + exerciseId + '/students');
    });
  }
}
