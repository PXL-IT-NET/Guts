import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { ChapterStatisticsModel } from '../viewmodels/chapter.model';

export class ChapterContext {

  private _statusDate: Date;
  public get statusDate() {
    return this._statusDate;
  }
  public set statusDate(date: any) {
    this._statusDate = new Date(date);
  }

  public courseId: number;
  public chapterCode: string;

  constructor() {
    this._statusDate = new Date();
  }
}

@Injectable()
export class ChapterContextProvider {
  private contextSource: Subject<ChapterContext>;
  private statisticsSource: Subject<void>;

  public contextChanged$: Observable<ChapterContext>;
  public statisticsChanged$: Observable<void>;
  public context: ChapterContext;
  public statistics: ChapterStatisticsModel;

  constructor() {
    this.contextSource = new Subject<ChapterContext>();
    this.contextChanged$ = this.contextSource.asObservable();
    this.context = new ChapterContext();

    this.statisticsSource = new Subject<void>();
    this.statisticsChanged$ = this.statisticsSource.asObservable();
    this.statistics = null;
  }

  public announceContextChange() {
    this.contextSource.next(this.context);
  }

  public announceStatisticsChange() {
    this.statisticsSource.next();
  }
}
