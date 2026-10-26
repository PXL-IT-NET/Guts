import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, Input, OnChanges, SimpleChanges, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { ProjectService } from "../../services/project.service";
import {
  TopicStatisticsModel,
  TopicSummaryModel,
} from "../../viewmodels/topic.model";
import { ToastrService } from "ngx-toastr";

@Component({
  standalone: false,
  selector: "app-project-summary",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./projectsummary.component.html",
})
export class ProjectSummaryComponent implements OnChanges {
  private readonly destroyRef = inject(DestroyRef);

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
    assignmentStatistics: [],
    assignments: [],
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

  @Input() public courseId: number;
  @Input() public teamId: number;
  @Input() public projectCode: string;
  @Input() public statusDate: Date;

  constructor(
    private readonly projectService: ProjectService,
    private readonly toastr: ToastrService,
  ) {
    this.courseId = 0;
    this.teamId = 0;
    this.projectCode = "";
    this.statusDate = new Date();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.courseId <= 0) return;
    let projectHasValue: boolean =
      this.projectCode && this.projectCode.length > 0;
    if (!projectHasValue) return;
    let projectHasChanged: boolean =
      (changes.projectCode ?? false) &&
      changes.projectCode.previousValue != this.projectCode;
    let statusDateHasChanged: boolean =
      (changes.statusDate ?? false) &&
      changes.statusDate.previousValue != this.statusDate;

    if (projectHasChanged || (statusDateHasChanged && projectHasValue)) {
      this.loadProjectStatistics();
    }

    if (this.teamId <= 0) return;
    this.loadProjectSummary();
  }

  private loadProjectSummary() {
    this.loadingSummary = true;
    this.projectService
      .getProjectSummary(
        this.courseId,
        this.projectCode,
        this.teamId,
        this.statusDate,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loadingSummary = false;
        if (result.success) {
          this.model = result.value;
        } else {
          this.toastr.error(
            "Could not load project summary from API. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  private loadProjectStatistics() {
    this.loadingStatistics = true;
    this.projectService
      .getProjectStatistics(this.courseId, this.projectCode, this.statusDate)
      .pipe(takeUntilDestroyed(this.destroyRef))
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


}
