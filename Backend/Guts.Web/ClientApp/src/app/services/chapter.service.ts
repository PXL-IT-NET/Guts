import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/mergeMap';
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { IChapterContentsModel, ChapterContentsModel } from "../viewmodels/chapter.model"

@Injectable()
export class ChapterService {
    private apiBaseUrl: string;

    constructor(private http: HttpClient,
        private settingsService: ClientSettingsService) {
        this.apiBaseUrl = '';
    }

    public getChapterSummary(courseId: number, chapterNumber: number): Observable<ChapterContentsModel> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http
                .get<IChapterContentsModel>(settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterNumber)
                .map((chapterContents: IChapterContentsModel) => {
                    return new ChapterContentsModel(chapterContents);
                });
        });
    }
}
