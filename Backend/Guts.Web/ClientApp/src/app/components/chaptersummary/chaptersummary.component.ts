import { Component, OnInit, OnDestroy } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { TopicContextProvider } from "../../services/topic.context.provider";
import { TopicStatisticsModel, TopicSummaryModel } from '../../viewmodels/topic.model';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import * as moment from 'moment';

@Component({
  templateUrl: './chaptersummary.component.html'
})
export class ChapterSummaryComponent implements OnInit, OnDestroy {
  public model: TopicSummaryModel;
  public statistics: TopicStatisticsModel;
  public loadingSummary: boolean = false;
  public loadingStatistics: boolean = false;

  private userId: number;
  private chapterContextSubscription: Subscription;
  private chapterStatisticsSubscription: Subscription;

  constructor(private chapterService: ChapterService,
    private topicContextProvider: TopicContextProvider,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
    this.model = {
      id: 0,
      code: '',
      description: '',
      assignmentSummaries: [],
    };
    this.statistics = {
      id: 0,
      code: '',
      description: '',
      assignmentStatistics: []
    };
    this.userId = 0;

    this.route.params.subscribe(params => {
      this.userId = +params['userId']; // (+) converts 'userId' to a number
      this.loadChapterSummary();
    });

    this.chapterContextSubscription = this.topicContextProvider.topicChanged$.subscribe(() => {
      this.loadChapterSummary();
    });

    this.loadingStatistics = true;
    this.chapterStatisticsSubscription = this.topicContextProvider.statisticsChanged$.subscribe(() => {
      this.loadingStatistics = false;
      this.statistics = this.topicContextProvider.currentContext.statistics;
    });
  }

  ngOnInit() {

  }

  ngOnDestroy() {
    this.chapterContextSubscription.unsubscribe();
    this.chapterStatisticsSubscription.unsubscribe();
    this.statistics = {
      id: 0,
      code: '',
      description: '',
      assignmentStatistics: []
    };
  }

  private loadChapterSummary() {
    if (!this.topicContextProvider.currentContext.topic) return;

    this.loadingSummary = true;
    this.chapterService.getChapterSummary(this.topicContextProvider.currentContext.courseId,
      this.topicContextProvider.currentContext.topic.code,
      this.userId,
      this.topicContextProvider.currentContext.statusDate)
      .subscribe((result) => {
        this.loadingSummary = false;

        if (result.success) {
          this.model = result.value;
        } else {
          this.toastr.error("Could not load chapter summary from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
  }
}
