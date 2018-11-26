import { Component, OnInit, OnDestroy } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ChapterContextProvider } from '../../services/chapter.context.provider';
import { ChapterSummaryModel, ChapterStatisticsModel } from '../../viewmodels/chapter.model';
import { ActivatedRoute } from '@angular/router';
import { ChapterContext } from '../../services/chapter.context.provider';
import { Subscription } from 'rxjs';


@Component({
  templateUrl: './chaptersummary.component.html'
})
export class ChapterSummaryComponent implements OnInit, OnDestroy {
  public model: ChapterSummaryModel;
  public statistics: ChapterStatisticsModel;

  private courseId: number;
  private chapterNumber: number;
  private userId: number;
  private chapterContextSubscription: Subscription;
  private chapterStatisticsSubscription: Subscription;

  constructor(private chapterService: ChapterService,
    private chapterContextProvider: ChapterContextProvider,
    private route: ActivatedRoute) {
    this.model = {
      id: 0,
      number: 0,
      exerciseSummaries: [],
    };
    this.statistics = {
      id: 0,
      number: 0,
      exerciseStatistics: []
    };
    this.courseId = 0;
    this.chapterNumber = 0;
    this.userId = 0;

    this.chapterContextSubscription = this.chapterContextProvider.contextChanged$.subscribe((context: ChapterContext) => {
      this.courseId = context.courseId;
      this.chapterNumber = context.chapterNumber;
      this.loadChapterSummary();
    });

    this.chapterStatisticsSubscription = this.chapterContextProvider.statisticsChanged$.subscribe(() => {
      this.statistics = this.chapterContextProvider.statistics;
    });

    this.courseId = this.chapterContextProvider.context.courseId;
    this.chapterNumber = this.chapterContextProvider.context.chapterNumber;

    this.route.params.subscribe(params => {
      this.userId = +params['userId']; // (+) converts 'userId' to a number
    });
  }

  ngOnInit() {
    
  }

  ngOnDestroy() {
    this.chapterContextSubscription.unsubscribe();
    this.chapterStatisticsSubscription.unsubscribe();
  }

  private loadChapterSummary() {
    this.chapterService.getChapterSummary(this.courseId, this.chapterNumber, this.userId, this.chapterContextProvider.context.statusDate).subscribe((chapterContents: ChapterSummaryModel) => {
      this.model = chapterContents;
    });
  }
}
