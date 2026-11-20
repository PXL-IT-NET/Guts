import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  Component,
  Input,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import { Subscription } from "rxjs";
import { AssignmentService } from "../../services/assignment.service";
import { ActivatedRoute } from "@angular/router";
import { formatDateTime } from "../../util/date.util";
import {
  IAssignmentDetailModel,
  AssignmentDetailModel,
} from "../../viewmodels/assignmentdetail.model";
import {
  IAssignmentTimelineModel,
  IAssignmentTimelinePointModel,
} from "../../viewmodels/assignment-timeline.model";

@Component({
  standalone: false,
  selector: "app-assignment-detail",
  templateUrl: "./assignmentdetail.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ["./assignmentdetail.component.css"],
})
export class AssignmentDetailComponent implements OnChanges {
  private readonly destroyRef = inject(DestroyRef);

  private detailSub?: Subscription;
  private timelineSub?: Subscription;
  private snapshotSub?: Subscription;
  private readonly snapshotCache = new Map<number, IAssignmentDetailModel>();

  private readonly _model = signal<AssignmentDetailModel>(
    new AssignmentDetailModel(),
  );
  public get model(): AssignmentDetailModel {
    return this._model();
  }
  public set model(v: AssignmentDetailModel) {
    this._model.set(v);
  }

  private readonly _loading = signal<boolean>(true);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public readonly timeline = signal<IAssignmentTimelineModel | null>(null);
  public readonly timelineLoading = signal<boolean>(false);
  public readonly timelineError = signal<string | null>(null);
  public readonly selectedRunIndex = signal<number>(0);
  public readonly snapshotLoading = signal<boolean>(false);

  @Input() public assignmentId = 0;
  @Input() public teamId = 0;
  @Input() public userId = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly assignmentService: AssignmentService,
  ) { }

  public get showTimeline(): boolean {
    const t = this.timeline();
    return !!t && t.runs.length > 1;
  }

  public get selectedRunPoint(): IAssignmentTimelinePointModel | null {
    const t = this.timeline();
    if (!t || t.runs.length === 0) {
      return null;
    }
    return t.runs[this.selectedRunIndex()] ?? null;
  }

  public get isViewingHistorical(): boolean {
    const t = this.timeline();
    return !!t && t.runs.length > 1 && this.selectedRunIndex() < t.runs.length - 1;
  }

  public get selectedRunTimestampFormatted(): string {
    const point = this.selectedRunPoint;
    return point ? formatDateTime(point.createDateTimeUtc) : "";
  }

  ngOnChanges(_changes: SimpleChanges) {
    if (this.assignmentId > 0 && (this.userId > 0 || this.teamId > 0)) {
      this.loadAssignment();
    }
  }

  public onTimelineRunSelected(index: number): void {
    const t = this.timeline();
    if (!t || index < 0 || index >= t.runs.length) {
      return;
    }
    const targetRun = t.runs[index];
    this.selectedRunIndex.set(index);

    const cached = this.snapshotCache.get(targetRun.testRunId);
    if (cached) {
      this.snapshotSub?.unsubscribe();
      this.snapshotLoading.set(false);
      this.applySnapshotPreservingExpandedFiles(cached);
      return;
    }

    this.snapshotSub?.unsubscribe();
    this.snapshotLoading.set(true);
    this.snapshotSub = this.assignmentService
      .getAssignmentDetail(
        this.assignmentId,
        this.userId,
        this.teamId,
        targetRun.createDateTimeUtc,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (snapshot: IAssignmentDetailModel) => {
          this.snapshotCache.set(targetRun.testRunId, snapshot);
          this.snapshotLoading.set(false);
          this.applySnapshotPreservingExpandedFiles(snapshot);
        },
        error: () => {
          this.snapshotLoading.set(false);
          this.timelineError.set(
            "Could not load the selected historical test run snapshot.",
          );
        },
      });
  }

  public backToLatest(): void {
    const t = this.timeline();
    if (!t || t.runs.length === 0) {
      return;
    }
    this.onTimelineRunSelected(t.runs.length - 1);
  }

  private resetTimelineState(): void {
    this.timelineSub?.unsubscribe();
    this.snapshotSub?.unsubscribe();
    this.snapshotCache.clear();
    this.timeline.set(null);
    this.timelineLoading.set(false);
    this.timelineError.set(null);
    this.selectedRunIndex.set(0);
    this.snapshotLoading.set(false);
  }

  private loadAssignment(): void {
    this.detailSub?.unsubscribe();
    this.resetTimelineState();
    this.loading = true;

    this.detailSub = this.assignmentService
      .getAssignmentDetail(this.assignmentId, this.userId, this.teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((assignmentDetail: IAssignmentDetailModel) => {
        this.loading = false;
        this.model = new AssignmentDetailModel(assignmentDetail);
        if (assignmentDetail && assignmentDetail.numberOfRuns > 1) {
          this.loadTimeline(assignmentDetail);
        }
      });
  }

  private loadTimeline(latestDetail: IAssignmentDetailModel): void {
    this.timelineLoading.set(true);
    this.timelineError.set(null);

    this.timelineSub = this.assignmentService
      .getAssignmentTimeline(this.assignmentId, this.userId, this.teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.timelineLoading.set(false);
        if (!result.success || !result.value) {
          this.timelineError.set(
            result.message || "Could not load test run timeline.",
          );
          return;
        }
        const timelineModel = result.value;
        if (timelineModel.runs.length <= 1) {
          this.timeline.set(null);
          return;
        }
        const latestIndex = timelineModel.runs.length - 1;
        this.snapshotCache.set(
          timelineModel.runs[latestIndex].testRunId,
          latestDetail,
        );
        this.timeline.set(timelineModel);
        this.selectedRunIndex.set(latestIndex);
      });
  }

  private applySnapshotPreservingExpandedFiles(
    snapshot: IAssignmentDetailModel,
  ): void {
    const expandedPaths = new Set(
      this.model.solutionFiles
        .filter((file) => !file.isCollapsed)
        .map((file) => file.filePath),
    );
    const nextModel = new AssignmentDetailModel(snapshot);
    for (const file of nextModel.solutionFiles) {
      if (expandedPaths.has(file.filePath)) {
        file.isCollapsed = false;
      }
    }
    this.model = nextModel;
  }
}
