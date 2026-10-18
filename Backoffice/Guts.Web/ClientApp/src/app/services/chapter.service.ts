import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { IChapterDetailsModel } from "../viewmodels/chapter.model";
import {
  TopicStatisticsModel,
  TopicSummaryModel,
  ITopicUpdateModel,
} from "../viewmodels/topic.model";
import { GetResult, PostResult } from "../util/result";
import { PeriodProvider } from "./period.provider";
import { TopicServiceBase } from "./topic.service.base";

@Injectable({
  providedIn: "root",
})
export class ChapterService extends TopicServiceBase {
  constructor(http: HttpClient, periodProvider: PeriodProvider) {
    super(http, periodProvider, "chapters");
  }

  public getChapterDetails(
    courseId: number,
    chapterCode: string,
  ): Observable<GetResult<IChapterDetailsModel>> {
    return this.getTopicDetails<IChapterDetailsModel>(courseId, chapterCode);
  }

  public updateChapter(
    courseId: number,
    chapterCode: string,
    model: ITopicUpdateModel,
  ): Observable<PostResult> {
    return this.updateTopic(courseId, chapterCode, model);
  }

  public getChapterSummary(
    courseId: number,
    chapterCode: string,
    userId: number,
    date?: Date | string,
  ): Observable<GetResult<TopicSummaryModel>> {
    return this.getTopicSummaryByTarget(
      courseId,
      chapterCode,
      "users",
      userId,
      date,
    );
  }

  public getChapterStatistics(
    courseId: number,
    chapterCode: string,
    date?: Date | string,
  ): Observable<GetResult<TopicStatisticsModel>> {
    return this.getTopicStatistics(courseId, chapterCode, date);
  }
}
