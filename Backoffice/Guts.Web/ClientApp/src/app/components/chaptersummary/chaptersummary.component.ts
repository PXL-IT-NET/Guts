import { Component, Input, SimpleChanges, OnChanges } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { TopicStatisticsModel, TopicSummaryModel } from '../../viewmodels/topic.model';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import * as moment from "moment";

@Component({
  selector: 'app-chapter-summary',
  templateUrl: './chaptersummary.component.html'
})
export class ChapterSummaryComponent implements OnChanges {
  public model: TopicSummaryModel;
  public statistics: TopicStatisticsModel;
  public loadingSummary: boolean = false;
  public loadingStatistics: boolean = false;

  @Input() public courseId: number;
  @Input() public userId: number;
  @Input() public chapterCode: string;
  @Input() public statusDate: moment.Moment;

  constructor(private chapterService: ChapterService,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
    this.model = new TopicSummaryModel();
    this.statistics = {
      id: 0,
      code: '',
      description: '',
      assignmentStatistics: []
    };
    this.courseId = 0;
    this.userId = 0;
    this.chapterCode = '';
    this.statusDate = moment();
    
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.courseId <= 0) return;
    let chapterHasValue: boolean = this.chapterCode && this.chapterCode.length > 0;
    if (!chapterHasValue) return;
    let chapterHasChanged: boolean = (changes.chapterCode ?? false) && changes.chapterCode.previousValue != this.chapterCode;
    let statusDateHasChanged: boolean = (changes.statusDate ?? false) && changes.statusDate.previousValue != this.statusDate;

    if (chapterHasChanged || (statusDateHasChanged && chapterHasValue)) {
      this.loadChapterStatistics();
    }

    if (this.userId <= 0) return;
    this.loadChapterSummary();
  }

  private loadChapterStatistics() {
    this.loadingStatistics = true;
    this.chapterService.getChapterStatistics(this.courseId, this.chapterCode, this.statusDate).subscribe((result) => {
      if (result.success) {
        this.statistics = result.value;
      } else {
        this.toastr.error("Could not load project statistics from API. Message: " + (result.message || "unknown error"), "API error");
      }
      this.loadingStatistics = false;
    });
  }

  private loadChapterSummary() {
    this.loadingSummary = true;
    this.chapterService.getChapterSummary(this.courseId,
      this.chapterCode,
      this.userId,
      this.statusDate)
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
