import { DestroyRef } from "@angular/core";
import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed, fakeAsync, tick } from "@angular/core/testing";
import { DeletionJobService } from "./deletion-job.service";
import { IDeletionJobStatusModel } from "../viewmodels/deletion-job.model";
import { GetResult } from "../util/result";

function createJob(
  overrides: Partial<IDeletionJobStatusModel> = {},
): IDeletionJobStatusModel {
  return {
    jobId: "job-123",
    targetType: "Course",
    targetId: 10,
    targetLabel: "NET1 - .NET Essentials",
    status: "Pending",
    totalRows: 100,
    deletedRows: 0,
    percentage: 0,
    currentPhase: "Queued",
    errorMessage: null,
    createdAtUtc: "2026-11-15T12:00:00Z",
    startedAtUtc: null,
    completedAtUtc: null,
    ...overrides,
  };
}

describe("DeletionJobService", () => {
  let service: DeletionJobService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        DeletionJobService,
      ],
    });
    service = TestBed.inject(DeletionJobService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should get job status and handle errors", () => {
    const job = createJob({ status: "InProgress", percentage: 45 });
    service.getJobStatus("job-123").subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.percentage).toBe(45);
    });
    httpMock.expectOne("api/deletion-jobs/job-123").flush(job);

    service.getJobStatus("job-404").subscribe((res) => {
      expect(res.success).toBeFalse();
    });
    httpMock
      .expectOne("api/deletion-jobs/job-404")
      .flush("Not found", { status: 404, statusText: "Not Found" });
  });

  it("should get active jobs with or without targetType and handle errors", () => {
    const jobs = [createJob({ status: "InProgress" })];

    service.getActiveJobs("Course").subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value).toHaveSize(1);
    });
    httpMock.expectOne("api/deletion-jobs/active?targetType=Course").flush(jobs);

    service.getActiveJobs().subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value).toHaveSize(0);
    });
    httpMock.expectOne("api/deletion-jobs/active").flush([]);

    service.getActiveJobs("Period").subscribe((res) => {
      expect(res.success).toBeFalse();
    });
    httpMock
      .expectOne("api/deletion-jobs/active?targetType=Period")
      .flush("Server Error", { status: 500, statusText: "Error" });
  });

  it("should poll until Completed status is reached", fakeAsync(() => {
    const emissions: GetResult<IDeletionJobStatusModel>[] = [];
    let completed = false;

    const sub = service.pollUntilComplete("job-123", 200).subscribe({
      next: (res) => emissions.push(res),
      complete: () => {
        completed = true;
      },
    });

    // t = 0: Pending
    tick(0);
    httpMock
      .expectOne("api/deletion-jobs/job-123")
      .flush(createJob({ status: "Pending", percentage: 0 }));
    expect(emissions).toHaveSize(1);
    expect(completed).toBeFalse();

    // t = 200: InProgress
    tick(200);
    httpMock
      .expectOne("api/deletion-jobs/job-123")
      .flush(createJob({ status: "InProgress", percentage: 60, deletedRows: 60 }));
    expect(emissions).toHaveSize(2);
    expect(completed).toBeFalse();

    // t = 400: Completed
    tick(200);
    httpMock
      .expectOne("api/deletion-jobs/job-123")
      .flush(createJob({ status: "Completed", percentage: 100, deletedRows: 100 }));
    expect(emissions).toHaveSize(3);
    expect(emissions[2].value.status).toBe("Completed");
    expect(completed).toBeTrue();

    sub.unsubscribe();
  }));

  it("should stop polling on Failed status or HTTP error", fakeAsync(() => {
    const failedEmissions: GetResult<IDeletionJobStatusModel>[] = [];
    let failedCompleted = false;

    service.pollUntilComplete("job-fail", 200).subscribe({
      next: (res) => failedEmissions.push(res),
      complete: () => {
        failedCompleted = true;
      },
    });

    tick(0);
    httpMock
      .expectOne("api/deletion-jobs/job-fail")
      .flush(createJob({ jobId: "job-fail", status: "Failed", errorMessage: "DB error" }));
    expect(failedEmissions).toHaveSize(1);
    expect(failedCompleted).toBeTrue();

    const errorEmissions: GetResult<IDeletionJobStatusModel>[] = [];
    let errorCompleted = false;
    service.pollUntilComplete("job-err", 200).subscribe({
      next: (res) => errorEmissions.push(res),
      complete: () => {
        errorCompleted = true;
      },
    });

    tick(0);
    httpMock
      .expectOne("api/deletion-jobs/job-err")
      .flush("Err", { status: 500, statusText: "Server Error" });
    expect(errorEmissions).toHaveSize(1);
    expect(errorEmissions[0].success).toBeFalse();
    expect(errorCompleted).toBeTrue();
  }));

  describe("DeletionProgressTracker", () => {
    function createStubDestroyRef(): {
      destroyRef: DestroyRef;
      triggerDestroy: () => void;
    } {
      const callbacks: Array<() => void> = [];
      const destroyRef = {
        onDestroy: (cb: () => void) => {
          callbacks.push(cb);
          return () => {};
        },
        destroyed: false,
      } as DestroyRef;
      return {
        destroyRef,
        triggerDestroy: () => callbacks.forEach((cb) => cb()),
      };
    }

    it("should track job updates, compute progressDetails, and call onCompleted", fakeAsync(() => {
      const { destroyRef, triggerDestroy } = createStubDestroyRef();
      const onCompleted = jasmine.createSpy("onCompleted");
      const onFailed = jasmine.createSpy("onFailed");

      const tracker = service.createTracker({
        destroyRef,
        targetType: "Course",
        entityNoun: "Course",
        onCompleted,
        onFailed,
      });

      expect(tracker.inProgress()).toBeFalse();
      expect(tracker.percentage()).toBe(0);
      expect(tracker.currentPhase()).toBe("");
      expect(tracker.progressDetails()).toBeNull();

      tracker.trackJob(createJob({ status: "Pending", totalRows: 50, deletedRows: 0 }), 200);
      expect(tracker.inProgress()).toBeTrue();
      expect(tracker.progressDetails()).toBe("0 / 50 rows removed");

      tick(0);
      httpMock
        .expectOne("api/deletion-jobs/job-123")
        .flush(
          createJob({
            status: "InProgress",
            totalRows: 50,
            deletedRows: 25,
            percentage: 50,
            currentPhase: "Deleting test runs",
          }),
        );
      expect(tracker.percentage()).toBe(50);
      expect(tracker.currentPhase()).toBe("Deleting test runs");
      expect(tracker.progressDetails()).toBe("25 / 50 rows removed");

      tick(200);
      httpMock
        .expectOne("api/deletion-jobs/job-123")
        .flush(createJob({ status: "Completed", percentage: 100, deletedRows: 50 }));

      expect(tracker.inProgress()).toBeFalse();
      expect(onCompleted).toHaveBeenCalled();
      expect(onFailed).not.toHaveBeenCalled();

      triggerDestroy();
    }));

    it("should handle immediate Completed, Failed, polling error, and clear()", fakeAsync(() => {
      const { destroyRef, triggerDestroy } = createStubDestroyRef();
      const onCompleted = jasmine.createSpy("onCompleted");
      const onFailed = jasmine.createSpy("onFailed");

      const tracker = service.createTracker({
        destroyRef,
        targetType: "Period",
        entityNoun: "Period",
        onCompleted,
        onFailed,
      });

      tracker.trackJob(createJob({ status: "Completed" }));
      expect(onCompleted).toHaveBeenCalledTimes(1);
      expect(tracker.inProgress()).toBeFalse();

      tracker.trackJob(createJob({ status: "Failed", errorMessage: "Immediate failure" }));
      expect(onFailed).toHaveBeenCalledWith("Immediate failure");

      tracker.trackJob(createJob({ status: "InProgress", totalRows: 0 }), 200);
      expect(tracker.progressDetails()).toBeNull();
      tick(0);
      httpMock
        .expectOne("api/deletion-jobs/job-123")
        .flush(createJob({ status: "Failed", errorMessage: null }));
      expect(onFailed).toHaveBeenCalledWith("Could not delete period.");

      tracker.trackJob(createJob({ status: "InProgress" }), 200);
      tick(0);
      httpMock
        .expectOne("api/deletion-jobs/job-123")
        .flush("Boom", { status: 500, statusText: "Err" });
      expect(onFailed).toHaveBeenCalledTimes(3);

      tracker.trackJob(createJob({ status: "InProgress" }), 200);
      expect(tracker.inProgress()).toBeTrue();
      tracker.clear();
      expect(tracker.inProgress()).toBeFalse();
      tick(0);

      triggerDestroy();
    }));

    it("should resume active jobs on resumeActive and ignore empty/errors/already-active", fakeAsync(() => {
      const { destroyRef, triggerDestroy } = createStubDestroyRef();
      const onCompleted = jasmine.createSpy("onCompleted");
      const onFailed = jasmine.createSpy("onFailed");

      const tracker = service.createTracker({
        destroyRef,
        targetType: "Course",
        entityNoun: "Course",
        onCompleted,
        onFailed,
      });

      // Empty active list
      tracker.resumeActive(200);
      httpMock.expectOne("api/deletion-jobs/active?targetType=Course").flush([]);
      expect(tracker.inProgress()).toBeFalse();

      // Error fetching active list
      tracker.resumeActive(200);
      httpMock
        .expectOne("api/deletion-jobs/active?targetType=Course")
        .flush("Err", { status: 500, statusText: "Err" });
      expect(tracker.inProgress()).toBeFalse();

      // Active job returned -> starts polling until complete
      tracker.resumeActive(200);
      httpMock
        .expectOne("api/deletion-jobs/active?targetType=Course")
        .flush([createJob({ jobId: "job-resume", status: "InProgress", percentage: 20 })]);
      expect(tracker.inProgress()).toBeTrue();
      expect(tracker.percentage()).toBe(20);

      tick(0);
      httpMock
        .expectOne("api/deletion-jobs/job-resume")
        .flush(createJob({ jobId: "job-resume", status: "Completed", percentage: 100 }));
      expect(tracker.inProgress()).toBeFalse();
      expect(onCompleted).toHaveBeenCalledTimes(1);

      triggerDestroy();
    }));
  });
});
