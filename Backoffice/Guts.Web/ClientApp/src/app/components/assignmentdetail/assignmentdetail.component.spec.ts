import { NO_ERRORS_SCHEMA, SimpleChange } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { Subject, of, throwError } from "rxjs";
import { AssignmentService } from "../../services/assignment.service";
import { createActivatedRouteProvider } from "../../testing/test-helpers";
import { GetResult } from "../../util/result";
import { IAssignmentDetailModel } from "../../viewmodels/assignmentdetail.model";
import { IAssignmentTimelineModel } from "../../viewmodels/assignment-timeline.model";
import { AssignmentDetailComponent } from "./assignmentdetail.component";

describe("AssignmentDetailComponent", () => {
  let component: AssignmentDetailComponent;
  let fixture: ComponentFixture<AssignmentDetailComponent>;
  let assignmentServiceSpy: jasmine.SpyObj<AssignmentService>;

  const createDetail = (
    overrides: Partial<IAssignmentDetailModel> = {},
  ): IAssignmentDetailModel => ({
    assignmentId: 4,
    code: "Ex1",
    description: "Exercise 1",
    topicCode: "Ch1",
    courseName: ".NET",
    courseId: 1,
    tests: [],
    testResults: [
      { testName: "TestA", passed: true, runned: true, message: "" },
    ],
    firstRun: "2026-11-20T10:00:00Z",
    lastRun: "2026-11-20T10:30:00Z",
    numberOfRuns: 2,
    solutionFiles: [
      {
        filePath: "Calc.cs",
        content: "// latest",
      },
      {
        filePath: "Program.cs",
        content: "// program",
      },
    ],
    ...overrides,
  });

  const sampleTimeline: IAssignmentTimelineModel = {
    assignmentId: 4,
    totalTestsCount: 2,
    runs: [
      {
        testRunId: 10,
        runNumber: 1,
        createDateTimeUtc: "2026-11-20T10:00:00Z",
        passedTestsCount: 0,
        failedTestsCount: 2,
        totalTestsCount: 2,
        testResultsChanged: true,
        solutionFilesChanged: true,
        testResultsDelta: 0,
      },
      {
        testRunId: 11,
        runNumber: 2,
        createDateTimeUtc: "2026-11-20T10:30:00Z",
        passedTestsCount: 2,
        failedTestsCount: 0,
        totalTestsCount: 2,
        testResultsChanged: true,
        solutionFilesChanged: true,
        testResultsDelta: 2,
      },
    ],
  };

  beforeEach(() => {
    assignmentServiceSpy = jasmine.createSpyObj<AssignmentService>(
      "AssignmentService",
      ["getAssignmentDetail", "getAssignmentTimeline"],
    );
    TestBed.overrideComponent(AssignmentDetailComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [AssignmentDetailComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider(),
        { provide: AssignmentService, useValue: assignmentServiceSpy },
      ],
    });
    fixture = TestBed.createComponent(AssignmentDetailComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it("ngOnChanges should load latest detail first and then asynchronously load timeline when numberOfRuns > 1", () => {
    const timelineSubject = new Subject<GetResult<IAssignmentTimelineModel>>();
    assignmentServiceSpy.getAssignmentDetail.and.returnValue(of(createDetail()));
    assignmentServiceSpy.getAssignmentTimeline.and.returnValue(
      timelineSubject.asObservable(),
    );

    component.assignmentId = 4;
    component.userId = 10;
    component.ngOnChanges({
      assignmentId: new SimpleChange(0, 4, true),
    });

    expect(assignmentServiceSpy.getAssignmentDetail).toHaveBeenCalledWith(
      4,
      10,
      0,
    );
    expect(assignmentServiceSpy.getAssignmentTimeline).toHaveBeenCalledWith(
      4,
      10,
      0,
    );
    expect(component.loading).toBeFalse();
    expect(component.model.assignmentId).toBe(4);
    expect(component.timelineLoading()).toBeTrue();
    expect(component.showTimeline).toBeFalse();

    timelineSubject.next(GetResult.success(sampleTimeline));

    expect(component.timelineLoading()).toBeFalse();
    expect(component.showTimeline).toBeTrue();
    expect(component.selectedRunIndex()).toBe(1);
    expect(component.isViewingHistorical).toBeFalse();
    expect(component.selectedRunPoint?.testRunId).toBe(11);
    expect(component.selectedRunTimestampFormatted).toContain("20/11/2026");
  });

  it("ngOnChanges should not load timeline when numberOfRuns <= 1", () => {
    assignmentServiceSpy.getAssignmentDetail.and.returnValue(
      of(createDetail({ numberOfRuns: 1 })),
    );

    component.assignmentId = 4;
    component.userId = 10;
    component.ngOnChanges({ assignmentId: new SimpleChange(0, 4, true) });

    expect(assignmentServiceSpy.getAssignmentTimeline).not.toHaveBeenCalled();
    expect(component.showTimeline).toBeFalse();
  });

  it("should hide timeline if API returns 1 or fewer runs and show warning on timeline failure", () => {
    assignmentServiceSpy.getAssignmentDetail.and.returnValue(of(createDetail()));
    assignmentServiceSpy.getAssignmentTimeline.and.returnValue(
      of(
        GetResult.success({
          assignmentId: 4,
          totalTestsCount: 2,
          runs: [sampleTimeline.runs[0]],
        }),
      ),
    );

    component.assignmentId = 4;
    component.userId = 10;
    component.ngOnChanges({ assignmentId: new SimpleChange(0, 4, true) });
    expect(component.showTimeline).toBeFalse();
    expect(component.timelineError()).toBeNull();

    const failed = new GetResult<IAssignmentTimelineModel>();
    failed.success = false;
    failed.message = "Timeline unavailable";
    assignmentServiceSpy.getAssignmentTimeline.and.returnValue(of(failed));

    component.ngOnChanges({ assignmentId: new SimpleChange(4, 4, false) });
    expect(component.showTimeline).toBeFalse();
    expect(component.timelineError()).toBe("Timeline unavailable");
    expect(component.model.assignmentId).toBe(4);
  });

  it("should fetch historical snapshot, preserve expanded solution files, cache snapshots, and jump back to latest", () => {
    const latestDetail = createDetail();
    const historicalDetail = createDetail({
      numberOfRuns: 1,
      testResults: [
        { testName: "TestA", passed: false, runned: true, message: "Failed" },
      ],
      solutionFiles: [
        {
          filePath: "Calc.cs",
          content: "// historical v1",
        },
        {
          filePath: "Program.cs",
          content: "// program",
        },
      ],
    });

    assignmentServiceSpy.getAssignmentDetail.and.callFake(
      (_aid, _uid, _tid, date) => {
        if (date === "2026-11-20T10:00:00Z") {
          return of(historicalDetail);
        }
        return of(latestDetail);
      },
    );
    assignmentServiceSpy.getAssignmentTimeline.and.returnValue(
      of(GetResult.success(sampleTimeline)),
    );

    component.assignmentId = 4;
    component.userId = 10;
    component.ngOnChanges({ assignmentId: new SimpleChange(0, 4, true) });

    // User expands Calc.cs
    component.model.solutionFiles[0].isCollapsed = false;
    expect(component.model.solutionFiles[1].isCollapsed).toBeTrue();

    // Scrub to historical run #1 (index 0)
    component.onTimelineRunSelected(0);

    expect(component.isViewingHistorical).toBeTrue();
    expect(component.model.testResults[0].passed).toBeFalse();
    expect(component.model.solutionFiles[0].content).toBe("// historical v1");
    // Expanded state preserved for Calc.cs; Program.cs remains collapsed
    expect(component.model.solutionFiles[0].isCollapsed).toBeFalse();
    expect(component.model.solutionFiles[1].isCollapsed).toBeTrue();
    expect(assignmentServiceSpy.getAssignmentDetail).toHaveBeenCalledTimes(2);

    // Jump back to latest -> served from cache (no 3rd HTTP call)
    component.backToLatest();
    expect(component.isViewingHistorical).toBeFalse();
    expect(component.model.solutionFiles[0].content).toBe("// latest");
    expect(component.model.solutionFiles[0].isCollapsed).toBeFalse();
    expect(assignmentServiceSpy.getAssignmentDetail).toHaveBeenCalledTimes(2);

    // Revisit run #1 -> also served from cache!
    component.onTimelineRunSelected(0);
    expect(component.model.solutionFiles[0].content).toBe("// historical v1");
    expect(assignmentServiceSpy.getAssignmentDetail).toHaveBeenCalledTimes(2);
  });

  it("should handle snapshot fetch errors and invalid selection indices safely", () => {
    assignmentServiceSpy.getAssignmentDetail.and.callFake(
      (_aid, _uid, _tid, date) => {
        if (date === "2026-11-20T10:00:00Z") {
          return throwError(() => new Error("Network error"));
        }
        return of(createDetail());
      },
    );
    assignmentServiceSpy.getAssignmentTimeline.and.returnValue(
      of(GetResult.success(sampleTimeline)),
    );

    // Before timeline loaded
    component.backToLatest();
    component.onTimelineRunSelected(0);
    expect(component.selectedRunPoint).toBeNull();
    expect(component.selectedRunTimestampFormatted).toBe("");

    component.assignmentId = 4;
    component.userId = 10;
    component.ngOnChanges({ assignmentId: new SimpleChange(0, 4, true) });

    // Out-of-bounds indices are ignored
    component.onTimelineRunSelected(-1);
    component.onTimelineRunSelected(99);
    expect(component.selectedRunIndex()).toBe(1);

    // Failing historical fetch sets warning and clears spinner
    component.onTimelineRunSelected(0);
    expect(component.snapshotLoading()).toBeFalse();
    expect(component.timelineError()).toContain("historical test run snapshot");
  });

  it("ngOnChanges should not load assignment when userId and teamId are 0", () => {
    component.assignmentId = 4;
    component.userId = 0;
    component.teamId = 0;

    component.ngOnChanges({});

    expect(assignmentServiceSpy.getAssignmentDetail).not.toHaveBeenCalled();
  });
});
