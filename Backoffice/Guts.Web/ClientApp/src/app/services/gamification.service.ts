import { Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { map, catchError } from "rxjs/operators";
import {
  IStudentGamificationProfileModel,
  IEnrollStudentModel,
  ILeaderboardEntryModel,
  IHallOfFameLeaderboardModel,
  IClassGroupLeaderboardEntryModel,
  ICourseGamificationConfigModel,
  ICourseClassGroupModel,
  IBadgeMetadataModel,
} from "../viewmodels/gamification.model";
import { GetResult, PostResult, CreateResult } from "../util/result";
import { PeriodProvider } from "./period.provider";

@Injectable({
  providedIn: "root",
})
export class GamificationService {
  constructor(
    private readonly http: HttpClient,
    private readonly periodProvider: PeriodProvider,
  ) {}

  private appendQueryParam(url: string, key: string, value?: number | null): string {
    if (value !== undefined && value !== null) {
      return url.includes("?") ? `${url}&${key}=${value}` : `${url}?${key}=${value}`;
    }
    return url;
  }

  private appendPeriod(url: string, periodId?: number): string {
    const pId = periodId ?? this.periodProvider.period?.id;
    return this.appendQueryParam(url, "periodId", pId);
  }

  public getProfile(
    courseId: number,
    periodId?: number,
  ): Observable<GetResult<IStudentGamificationProfileModel>> {
    const url = this.appendPeriod(`api/courses/${courseId}/gamification/profile`, periodId);
    return this.http
      .get<IStudentGamificationProfileModel>(url)
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<IStudentGamificationProfileModel>(err)),
        ),
      );
  }

  public enroll(
    courseId: number,
    enrollDto: IEnrollStudentModel,
    periodId?: number,
  ): Observable<GetResult<IStudentGamificationProfileModel>> {
    const url = this.appendPeriod(`api/courses/${courseId}/gamification/enroll`, periodId);
    return this.http
      .post<IStudentGamificationProfileModel>(url, enrollDto)
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<IStudentGamificationProfileModel>(err)),
        ),
      );
  }

  public getWeeklySprint(
    courseId: number,
    periodId?: number,
    perspectiveUserId?: number | null,
  ): Observable<GetResult<ILeaderboardEntryModel[]>> {
    let url = this.appendPeriod(`api/courses/${courseId}/gamification/leaderboard/weekly`, periodId);
    url = this.appendQueryParam(url, "perspectiveUserId", perspectiveUserId);
    return this.http
      .get<ILeaderboardEntryModel[]>(url)
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<ILeaderboardEntryModel[]>(err)),
        ),
      );
  }

  public getHallOfFame(
    courseId: number,
    periodId?: number,
    perspectiveUserId?: number | null,
    tier?: number | null,
  ): Observable<GetResult<IHallOfFameLeaderboardModel>> {
    let url = this.appendPeriod(`api/courses/${courseId}/gamification/leaderboard/hall-of-fame`, periodId);
    url = this.appendQueryParam(url, "perspectiveUserId", perspectiveUserId);
    url = this.appendQueryParam(url, "tier", tier);
    return this.http
      .get<IHallOfFameLeaderboardModel>(url)
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<IHallOfFameLeaderboardModel>(err)),
        ),
      );
  }

  public getClassLeaderboard(
    courseId: number,
    periodId?: number,
  ): Observable<GetResult<IClassGroupLeaderboardEntryModel[]>> {
    const url = this.appendPeriod(`api/courses/${courseId}/gamification/leaderboard/classes`, periodId);
    return this.http
      .get<IClassGroupLeaderboardEntryModel[]>(url)
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<IClassGroupLeaderboardEntryModel[]>(err)),
        ),
      );
  }

  public getConfig(
    courseId: number,
  ): Observable<GetResult<ICourseGamificationConfigModel>> {
    return this.http
      .get<ICourseGamificationConfigModel>(
        `api/courses/${courseId}/gamification/config`,
      )
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<ICourseGamificationConfigModel>(err)),
        ),
      );
  }

  public updateConfig(
    courseId: number,
    config: ICourseGamificationConfigModel,
  ): Observable<GetResult<ICourseGamificationConfigModel>> {
    return this.http
      .put<ICourseGamificationConfigModel>(
        `api/courses/${courseId}/gamification/config`,
        config,
      )
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<ICourseGamificationConfigModel>(err)),
        ),
      );
  }

  public getClasses(
    courseId: number,
    periodId?: number,
  ): Observable<GetResult<ICourseClassGroupModel[]>> {
    const url = this.appendPeriod(`api/courses/${courseId}/gamification/classes`, periodId);
    return this.http
      .get<ICourseClassGroupModel[]>(url)
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<ICourseClassGroupModel[]>(err)),
        ),
      );
  }

  public addClass(
    courseId: number,
    name: string,
    periodId?: number,
  ): Observable<CreateResult<ICourseClassGroupModel>> {
    const url = this.appendPeriod(`api/courses/${courseId}/gamification/classes`, periodId);
    return this.http
      .post<ICourseClassGroupModel>(
        url,
        JSON.stringify(name),
        {
          headers: { "Content-Type": "application/json" },
        },
      )
      .pipe(
        map((model) => CreateResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(CreateResult.fromHttpErrorResponse<ICourseClassGroupModel>(err)),
        ),
      );
  }

  public deleteClass(
    courseId: number,
    classGroupId: number,
  ): Observable<PostResult> {
    return this.http
      .delete(`api/courses/${courseId}/gamification/classes/${classGroupId}`)
      .pipe(
        map(() => PostResult.success()),
        catchError((err: HttpErrorResponse) =>
          of(PostResult.fromHttpErrorResponse(err)),
        ),
      );
  }

  public getBadgeMetadata(): Observable<GetResult<IBadgeMetadataModel[]>> {
    return this.http
      .get<IBadgeMetadataModel[]>("api/gamification/badges")
      .pipe(
        map((model) => GetResult.success(model)),
        catchError((err: HttpErrorResponse) =>
          of(GetResult.fromHttpErrorResponse<IBadgeMetadataModel[]>(err)),
        ),
      );
  }
}
