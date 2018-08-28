import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IExerciseDetailModel } from "../viewmodels/exercisedetail.model"

@Injectable()
export class ExerciseService {
  private apiBaseUrl: string;

  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
    this.apiBaseUrl = '';
  }

  public getExerciseDetail(exerciseId: number): Observable<IExerciseDetailModel> {
    return this.settingsService.get().mergeMap((settings: ClientSettings) => {
      return this.http.get<IExerciseDetailModel>(settings.apiBaseUrl + 'api/exercises/' + exerciseId);
    });
  }
}
