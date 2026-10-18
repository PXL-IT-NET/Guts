import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { CreateResult, GetResult, PostResult } from "../util/result";
import {
  ITopicStatisticsModel,
  ITopicSummaryModel,
  ITopicUpdateModel,
  TopicStatisticsModel,
  TopicSummaryModel,
} from "../viewmodels/topic.model";
import { PeriodProvider } from "./period.provider";

export abstract class TopicServiceBase {
  protected constructor(
    protected http: HttpClient,
    protected periodProvider: PeriodProvider,
    private readonly topicSegment: "chapters" | "projects",
  ) {}

  protected getCourseTopicsUrl(courseId: number): string {
    return `api/courses/${courseId}/${this.topicSegment}`;
  }

  protected getTopicUrl(
    courseId: number,
    topicCode: string,
    suffix = "",
  ): string {
    return `${this.getCourseTopicsUrl(courseId)}/${topicCode}${suffix}`;
  }

  protected appendPeriodAndDateQuery(
    baseUrl: string,
    date?: Date | string,
  ): string {
    let apiUrl = baseUrl;
    const hasPeriod = Boolean(this.periodProvider.period);
    if (hasPeriod) {
      apiUrl += `?periodId=${this.periodProvider.period.id}`;
    }
    if (date) {
      const dateIso = date instanceof Date ? date.toISOString() : date;
      apiUrl += `${hasPeriod ? "&" : "?"}date=${dateIso}`;
    }
    return apiUrl;
  }

  protected get<T>(url: string): Observable<GetResult<T>> {
    return this.getMapped<T, T>(url, (model) => model);
  }

  protected getMapped<TDto, TModel>(
    url: string,
    mapper: (dto: TDto) => TModel,
  ): Observable<GetResult<TModel>> {
    return this.http.get<TDto>(url).pipe(
      map((dto) => GetResult.success(mapper(dto))),
      catchError((errorResponse: HttpErrorResponse) =>
        of(GetResult.fromHttpErrorResponse<TModel>(errorResponse)),
      ),
    );
  }

  protected postCreate<T>(
    url: string,
    body: unknown,
  ): Observable<CreateResult<T>> {
    return this.http.post<T>(url, body).pipe(
      map((created) => CreateResult.success(created)),
      catchError((errorResponse: HttpErrorResponse) =>
        of(CreateResult.fromHttpErrorResponse<T>(errorResponse)),
      ),
    );
  }

  protected postAction(url: string, body: unknown = {}): Observable<PostResult> {
    return this.toPostResult(this.http.post(url, body));
  }

  protected putAction(url: string, body: unknown): Observable<PostResult> {
    return this.toPostResult(this.http.put(url, body));
  }

  protected deleteAction(url: string): Observable<PostResult> {
    return this.toPostResult(this.http.delete(url, {}));
  }

  protected getTopicDetails<TDetails>(
    courseId: number,
    topicCode: string,
  ): Observable<GetResult<TDetails>> {
    const apiUrl = this.appendPeriodAndDateQuery(
      this.getTopicUrl(courseId, topicCode),
    );
    return this.get<TDetails>(apiUrl);
  }

  protected updateTopic(
    courseId: number,
    topicCode: string,
    model: ITopicUpdateModel,
  ): Observable<PostResult> {
    return this.putAction(this.getTopicUrl(courseId, topicCode), model);
  }

  protected getTopicSummaryByTarget(
    courseId: number,
    topicCode: string,
    targetSegment: "users" | "teams",
    targetId: number,
    date?: Date | string,
  ): Observable<GetResult<TopicSummaryModel>> {
    const apiUrl = this.appendPeriodAndDateQuery(
      this.getTopicUrl(
        courseId,
        topicCode,
        `/${targetSegment}/${targetId}/summary`,
      ),
      date,
    );
    return this.getMapped<ITopicSummaryModel, TopicSummaryModel>(
      apiUrl,
      (model) => new TopicSummaryModel(model),
    );
  }

  protected getTopicStatistics(
    courseId: number,
    topicCode: string,
    date?: Date | string,
  ): Observable<GetResult<TopicStatisticsModel>> {
    const apiUrl = this.appendPeriodAndDateQuery(
      this.getTopicUrl(courseId, topicCode, "/statistics"),
      date,
    );
    return this.getMapped<ITopicStatisticsModel, TopicStatisticsModel>(
      apiUrl,
      (statistics) => new TopicStatisticsModel(statistics),
    );
  }

  private toPostResult(request$: Observable<Object>): Observable<PostResult> {
    return request$.pipe(
      map(() => PostResult.success()),
      catchError((errorResponse: HttpErrorResponse) =>
        of(PostResult.fromHttpErrorResponse(errorResponse)),
      ),
    );
  }
}

