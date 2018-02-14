import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import 'rxjs/add/operator/map'
import { ClientSettingsService } from './client.settings.service';
import { ClientSettings } from './client.settings';
import { ICourseModel, ICourseContentsModel } from "../viewmodels/course.model"

@Injectable()
export class CourseService {
    private apiBaseUrl: string;

    constructor(private http: HttpClient,
        private settingsService: ClientSettingsService) {
        this.apiBaseUrl = '';
    }

    public getCourses(): Observable<ICourseModel[]> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.get<ICourseModel[]>(settings.apiBaseUrl + 'api/courses');
        });
    }

    public getCourseContentsById(courseId: number): Observable<ICourseContentsModel> {
        return this.settingsService.get().mergeMap((settings: ClientSettings) => {
            return this.http.get<ICourseContentsModel>(settings.apiBaseUrl + 'api/courses/' + courseId);
        });
    }
}