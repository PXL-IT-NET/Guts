import { Component, OnInit, OnDestroy } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ChapterContextProvider } from '../../services/chapter.context.provider';
import { ChapterSummaryModel, ChapterStatisticsModel } from '../../viewmodels/chapter.model';
import { ActivatedRoute } from '@angular/router';
import { ChapterContext } from '../../services/chapter.context.provider';
import { Subscription } from 'rxjs';
import { ToastrService } from 'ngx-toastr';


@Component({
  templateUrl: './chaptersummary.component.html'
})
export class ChapterSummaryComponent implements OnInit, OnDestroy {
  public model: ChapterSummaryModel;
  public statistics: ChapterStatisticsModel;
  public loadingSummary: boolean = false;
  public loadingStatistics: boolean = false;

  private courseId: number;
  private chapterNumber: number;
  private userId: number;
  private chapterContextSubscription: Subscription;
  private chapterStatisticsSubscription: Subscription;

  constructor(private chapterService: ChapterService,
    private chapterContextProvider: ChapterContextProvider,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
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

    this.loadingStatistics = true;
    this.chapterContextSubscription = this.chapterContextProvider.contextChanged$.subscribe((context: ChapterContext) => {
      this.courseId = context.courseId;
      this.chapterNumber = context.chapterNumber;
      this.loadChapterSummary();

      if (this.chapterContextProvider.statistics && this.chapterContextProvider.statistics.number === this.chapterNumber) {
        this.loadingStatistics = false;
        this.statistics = this.chapterContextProvider.statistics;
      }
    });

    this.chapterStatisticsSubscription = this.chapterContextProvider.statisticsChanged$.subscribe(() => {
      this.loadingStatistics = false;
      this.statistics = this.chapterContextProvider.statistics;
    });

    this.route.params.subscribe(params => {
      this.userId = +params['userId']; // (+) converts 'userId' to a number
    });
  }

  ngOnInit() {
    
  }

  ngOnDestroy() {
    this.chapterContextSubscription.unsubscribe();
    this.chapterStatisticsSubscription.unsubscribe();
    this.statistics = {
      id: 0,
      number: 0,
      exerciseStatistics: []
    };
  }

  private loadChapterSummary() {
    this.loadingSummary = true;
    this.chapterService.getChapterSummary(this.courseId, this.chapterNumber, this.userId, this.chapterContextProvider.context.statusDate)
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
