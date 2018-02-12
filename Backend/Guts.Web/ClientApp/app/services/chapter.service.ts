import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/map'
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { ChapterContentsModel } from "../models/chaptercontents.model"

@Injectable()
export class ChapterService {
    private apiBaseUrl: string;

    constructor(private http: HttpClient,
        private settingsService: ClientSettingsService) {
        this.apiBaseUrl = '';
    }

    public getChapterSummary(courseId: number, chapterId: number): Observable<ChapterContentsModel | null> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.get<ChapterContentsModel>(settings.apiBaseUrl + 'api/courses/' + courseId + '/chapters/' + chapterId);
        });
    }
}