import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IChapterSummaryModel, ChapterSummaryModel, IChapterDetailsModel } from "../viewmodels/chapter.model"

@Injectable()
export class ChapterService {
  constructor(private http: HttpClient,
    private settingsService: ClientSettingsService) {
  }

  public getChapterDetails(courseId: number, chapterNumber: number): Observable<IChapterDetailsModel> {
    return this.settingsService.get().mergeMap<ClientSettings, IChapterDetailsModel>((settings: ClientSettings) => {
      return this.http
        .get<IChapterDetailsModel>(settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber);
    });
  }

  public getChapterSummary(courseId: number, chapterNumber: number, userId: number): Observable<ChapterSummaryModel> {
    return this.settingsService.get().mergeMap<ClientSettings, ChapterSummaryModel>((settings: ClientSettings) => {
      return this.http
        .get<IChapterSummaryModel>(settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber + '/users/' + userId + '/summary')
        .map((chapterContents: IChapterSummaryModel) => {
          return new ChapterSummaryModel(chapterContents);
        });
    });
  }
}
