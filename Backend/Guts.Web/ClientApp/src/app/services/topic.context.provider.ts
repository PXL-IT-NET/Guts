import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { TopicStatisticsModel } from '../viewmodels/topic.model';
import { ITopicModel } from "../viewmodels/topic.model";
import * as moment from "moment";

export class TopicContext {
  public courseId: number;

  public statusDate : moment.Moment;

  //private _statusDate: Date;
  //public get statusDate() {
  //  return this._statusDate;
  //}
  //public set statusDate(date: any) {
  //  this._statusDate = new Date(date);
  //}

  public topic: ITopicModel;

  public statistics: TopicStatisticsModel;

  constructor() {
    this.courseId = 0;
    //this._statusDate = new Date();
    this.statusDate = moment();
    this.topic = null;
    this.statistics = null;
  }
}

@Injectable()
export class TopicContextProvider {
  private topicSource: Subject<void>;
  private statisticsSource: Subject<void>;

  public topicChanged$: Observable<void>;
  public statisticsChanged$: Observable<void>;

  private _currentContext: TopicContext;
  public get currentContext() {
    return this._currentContext;
  }

  constructor() {
    this.topicSource = new Subject<void>();
    this.topicChanged$ = this.topicSource.asObservable();
    this._currentContext = new TopicContext();

    this.statisticsSource = new Subject<void>();
    this.statisticsChanged$ = this.statisticsSource.asObservable();
  }

  public setTopic(courseId: number, topic: ITopicModel, statusDate: moment.Moment) {
    var topicChanged = false;
    if (this._currentContext.courseId !== courseId) {
      this._currentContext.courseId = courseId;
      topicChanged = true;
    }
    if (!this._currentContext.topic || this._currentContext.topic.id !== topic.id) {
      this._currentContext.topic = topic;
      topicChanged = true;
    }
    if (!this._currentContext.statusDate || !this._currentContext.statusDate.isSame(statusDate)) {
      this._currentContext.statusDate = statusDate;
      topicChanged = true;
    }
    if (topicChanged) {
      this.topicSource.next();
    }
  }

  public setStatistics(statistics: TopicStatisticsModel) {
    this._currentContext.statistics = statistics;
    this.statisticsSource.next();
  }

  public resendStatistics() {
    this.statisticsSource.next();
  }
}
