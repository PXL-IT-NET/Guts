import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IChapterSummaryModel, ChapterSummaryModel, IChapterDetailsModel, IChapterStatisticsModel, ChapterStatisticsModel } from "../viewmodels/chapter.model"

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

  public getChapterSummary(courseId: number, chapterNumber: number, userId: number, date?: Date): Observable<ChapterSummaryModel> {
    return this.settingsService.get().mergeMap<ClientSettings, ChapterSummaryModel>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber + '/users/' + userId + '/summary';
      if (date) {
        apiUrl += '?date=' + date.toISOString();
      }
      return this.http.get<IChapterSummaryModel>(apiUrl)
        .map<IChapterSummaryModel, ChapterSummaryModel>((chapterContents: IChapterSummaryModel) => {
          return new ChapterSummaryModel(chapterContents);
        });
    });
  }

  public getChapterStatistics(courseId: number, chapterNumber: number, date?: Date): Observable<ChapterStatisticsModel> {
    return this.settingsService.get().mergeMap<ClientSettings, ChapterStatisticsModel>((settings: ClientSettings) => {
      var apiUrl = settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber + '/statistics';
      if (date) {
        apiUrl += '?date=' + date.toISOString();
      }
      return this.http.get<IChapterStatisticsModel>(apiUrl)
        .map<IChapterStatisticsModel, ChapterStatisticsModel>((chapterStatistics: IChapterStatisticsModel) => {
          return new ChapterStatisticsModel(chapterStatistics);
        });
    });
  }
}
