import {
  Component,
  Input,
  SimpleChanges,
  OnChanges,
  ChangeDetectionStrategy,
  signal
} from "@angular/core";
import { ChapterService } from "../../services/chapter.service";
import {
  TopicStatisticsModel,
  TopicSummaryModel,
} from "../../viewmodels/topic.model";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";

@Component({
  standalone: false,
  selector: "app-chapter-summary",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./chaptersummary.component.html",
})
export class ChapterSummaryComponent implements OnChanges {
  private readonly _model = signal<TopicSummaryModel>(new TopicSummaryModel());
  public get model(): TopicSummaryModel {
    return this._model();
  }
  public set model(v: TopicSummaryModel) {
    this._model.set(v);
  }

  private readonly _statistics = signal<TopicStatisticsModel>({
    id: 0,
    code: "",
    description: "",
    assignments: [],
    assignmentStatistics: [],
  });
  public get statistics(): TopicStatisticsModel {
    return this._statistics();
  }
  public set statistics(v: TopicStatisticsModel) {
    this._statistics.set(v);
  }

  private readonly _loadingSummary = signal<boolean>(false);
  public get loadingSummary(): boolean {
    return this._loadingSummary();
  }
  public set loadingSummary(v: boolean) {
    this._loadingSummary.set(v);
  }

  private readonly _loadingStatistics = signal<boolean>(false);
  public get loadingStatistics(): boolean {
    return this._loadingStatistics();
  }
  public set loadingStatistics(v: boolean) {
    this._loadingStatistics.set(v);
  }

  @Input() public courseId = 0;
  @Input() public userId = 0;
  @Input() public chapterCode = "";
  @Input() public statusDate: Date = new Date();

  constructor(
    private readonly chapterService: ChapterService,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService,
  ) {}

  ngOnChanges(changes: SimpleChanges) {
    if (!this.courseId || this.courseId <= 0) return;
    let chapterHasValue: boolean =
      this.chapterCode && this.chapterCode.length > 0;
    if (!chapterHasValue) return;
    let chapterHasChanged: boolean =
      (changes.chapterCode ?? false) &&
      changes.chapterCode.previousValue != this.chapterCode;
    let statusDateHasChanged: boolean =
      (changes.statusDate ?? false) &&
      changes.statusDate.previousValue != this.statusDate;

    if (chapterHasChanged || (statusDateHasChanged && chapterHasValue)) {
      this.loadChapterStatistics();
    }

    if (!this.userId || this.userId <= 0) return;
    this.loadChapterSummary();
  }

  private loadChapterStatistics() {
    this.loadingStatistics = true;

    this.chapterService
      .getChapterStatistics(this.courseId, this.chapterCode, this.statusDate)
      .subscribe((result) => {
        if (result.success) {
          this.statistics = result.value;
        } else {
          this.toastr.error(
            "Could not load project statistics from API. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
        this.loadingStatistics = false;
      });
  }

  private loadChapterSummary() {
    this.loadingSummary = true;
    this.chapterService
      .getChapterSummary(
        this.courseId,
        this.chapterCode,
        this.userId,
        this.statusDate,
      )
      .subscribe((result) => {
        if (result.success) {
          this.model = result.value;
        } else {
          this.toastr.error(
            "Could not load chapter summary from API. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
        this.loadingSummary = false;
      });
  }
}
