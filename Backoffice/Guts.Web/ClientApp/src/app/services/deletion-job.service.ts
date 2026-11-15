import { DestroyRef, Injectable, Signal, computed, signal } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { Observable, Subscription, of, timer } from "rxjs";
import { catchError, map, switchMap, takeWhile } from "rxjs/operators";
import {
  DeletionTargetType,
  IDeletionJobStatusModel,
} from "../viewmodels/deletion-job.model";
import { GetResult } from "../util/result";

export interface IDeletionProgressTrackerConfig {
  destroyRef: DestroyRef;
  targetType: DeletionTargetType;
  entityNoun: string;
  onCompleted: (job: IDeletionJobStatusModel) => void;
  onFailed: (message: string) => void;
}

export class DeletionProgressTracker {
  public readonly activeJob = signal<IDeletionJobStatusModel | null>(null);
  public readonly inProgress: Signal<boolean> = computed(
    () => this.activeJob() !== null,
  );
  public readonly percentage: Signal<number> = computed(
    () => this.activeJob()?.percentage ?? 0,
  );
  public readonly currentPhase: Signal<string> = computed(
    () => this.activeJob()?.currentPhase ?? "",
  );
  public readonly progressDetails: Signal<string | null> = computed(() => {
    const job = this.activeJob();
    if (!job || job.totalRows <= 0) {
      return null;
    }
    return `${job.deletedRows} / ${job.totalRows} rows removed`;
  });

  private pollSub?: Subscription;
  private resumeSub?: Subscription;

  constructor(
    private readonly service: DeletionJobService,
    private readonly config: IDeletionProgressTrackerConfig,
  ) {
    this.config.destroyRef.onDestroy(() => this.clear());
  }

  public trackJob(initialJob: IDeletionJobStatusModel, intervalMs = 500): void {
    this.activeJob.set(initialJob);

    if (initialJob.status === "Completed") {
      this.handleCompleted(initialJob);
      return;
    }

    if (initialJob.status === "Failed") {
      this.handleFailed(initialJob.errorMessage);
      return;
    }

    this.pollSub?.unsubscribe();
    this.pollSub = this.service
      .pollUntilComplete(initialJob.jobId, intervalMs)
      .subscribe((result) => {
        if (!result.success) {
          this.handleFailed(result.message);
          return;
        }

        const job = result.value;
        if (job.status === "Completed") {
          this.handleCompleted(job);
        } else if (job.status === "Failed") {
          this.handleFailed(job.errorMessage);
        } else {
          this.activeJob.set(job);
        }
      });
  }

  public resumeActive(intervalMs = 500): void {
    this.resumeSub?.unsubscribe();
    this.resumeSub = this.service
      .getActiveJobs(this.config.targetType)
      .subscribe((result) => {
        if (result.success && result.value.length > 0 && !this.inProgress()) {
          this.trackJob(result.value[0], intervalMs);
        }
      });
  }

  public clear(): void {
    this.pollSub?.unsubscribe();
    this.pollSub = undefined;
    this.resumeSub?.unsubscribe();
    this.resumeSub = undefined;
    this.activeJob.set(null);
  }

  private handleCompleted(job: IDeletionJobStatusModel): void {
    this.pollSub?.unsubscribe();
    this.pollSub = undefined;
    this.activeJob.set(null);
    this.config.onCompleted(job);
  }

  private handleFailed(message?: string | null): void {
    this.pollSub?.unsubscribe();
    this.pollSub = undefined;
    this.activeJob.set(null);
    this.config.onFailed(
      message || `Could not delete ${this.config.entityNoun.toLowerCase()}.`,
    );
  }
}

@Injectable({
  providedIn: "root",
})
export class DeletionJobService {
  constructor(private readonly http: HttpClient) {}

  public getJobStatus(
    jobId: string,
  ): Observable<GetResult<IDeletionJobStatusModel>> {
    return this.http
      .get<IDeletionJobStatusModel>(`api/deletion-jobs/${jobId}`)
      .pipe(
        map((model) => GetResult.success<IDeletionJobStatusModel>(model)),
        catchError((errorResponse: HttpErrorResponse) =>
          of(
            GetResult.fromHttpErrorResponse<IDeletionJobStatusModel>(
              errorResponse,
            ),
          ),
        ),
      );
  }

  public getActiveJobs(
    targetType?: DeletionTargetType,
  ): Observable<GetResult<IDeletionJobStatusModel[]>> {
    const url = targetType
      ? `api/deletion-jobs/active?targetType=${targetType}`
      : "api/deletion-jobs/active";

    return this.http.get<IDeletionJobStatusModel[]>(url).pipe(
      map((models) => GetResult.success<IDeletionJobStatusModel[]>(models)),
      catchError((errorResponse: HttpErrorResponse) =>
        of(
          GetResult.fromHttpErrorResponse<IDeletionJobStatusModel[]>(
            errorResponse,
          ),
        ),
      ),
    );
  }

  public pollUntilComplete(
    jobId: string,
    intervalMs = 500,
  ): Observable<GetResult<IDeletionJobStatusModel>> {
    return timer(0, intervalMs).pipe(
      switchMap(() => this.getJobStatus(jobId)),
      takeWhile(
        (result) =>
          result.success &&
          result.value.status !== "Completed" &&
          result.value.status !== "Failed",
        true,
      ),
    );
  }

  public createTracker(
    config: IDeletionProgressTrackerConfig,
  ): DeletionProgressTracker {
    return new DeletionProgressTracker(this, config);
  }
}
