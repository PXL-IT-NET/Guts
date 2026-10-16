import { ProjectSummaryComponent } from "./projectsummary.component";
import { ProjectService } from "../../services/project.service";
import { GetResult } from "../../util/result";
import {
  TopicStatisticsModel,
  TopicSummaryModel,
} from "../../viewmodels/topic.model";
import { SimpleChange } from "@angular/core";
import moment from "moment";
import { of } from "rxjs";
import {
  createChangeDetectorRefSpy,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("ProjectSummaryComponent", () => {
  let component: ProjectSummaryComponent;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let toastrSpy: ReturnType<typeof createToastrSpy>;
  let cdrSpy: ReturnType<typeof createChangeDetectorRefSpy>;

  beforeEach(() => {
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "getProjectSummary",
      "getProjectStatistics",
    ]);
    toastrSpy = createToastrSpy();
    cdrSpy = createChangeDetectorRefSpy();

    component = new ProjectSummaryComponent(
      projectServiceSpy,
      toastrSpy,
      cdrSpy,
    );
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("should do nothing in ngOnChanges when courseId <= 0 or projectCode is empty", () => {
    component.courseId = 0;
    component.projectCode = "PROJ1";
    component.ngOnChanges({
      projectCode: new SimpleChange("", "PROJ1", true),
    });
    expect(projectServiceSpy.getProjectStatistics).not.toHaveBeenCalled();

    component.courseId = 1;
    component.projectCode = "";
    component.ngOnChanges({
      courseId: new SimpleChange(0, 1, false),
    });
    expect(projectServiceSpy.getProjectStatistics).not.toHaveBeenCalled();
  });

  it("should load project statistics and summary when projectCode changes and teamId > 0", () => {
    const stats = new TopicStatisticsModel({
      id: 1,
      code: "PROJ1",
      description: "Project 1",
      assignments: [],
      assignmentStatistics: [],
    });
    const summary = new TopicSummaryModel();
    summary.id = 1;

    projectServiceSpy.getProjectStatistics.and.returnValue(
      of(GetResult.success(stats)),
    );
    projectServiceSpy.getProjectSummary.and.returnValue(
      of(GetResult.success(summary)),
    );

    const date = moment("2025-05-01T12:00:00Z");
    component.courseId = 1;
    component.projectCode = "PROJ1";
    component.teamId = 10;
    component.statusDate = date;

    component.ngOnChanges({
      projectCode: new SimpleChange("", "PROJ1", true),
    });

    expect(projectServiceSpy.getProjectStatistics).toHaveBeenCalledWith(
      1,
      "PROJ1",
      date,
    );
    expect(projectServiceSpy.getProjectSummary).toHaveBeenCalledWith(
      1,
      "PROJ1",
      10,
      date,
    );
    expect(component.statistics).toBe(stats);
    expect(component.model).toBe(summary);
    expect(component.loadingStatistics).toBeFalse();
    expect(component.loadingSummary).toBeFalse();
  });

  it("should load statistics when statusDate changes and not load summary when teamId <= 0", () => {
    const stats = new TopicStatisticsModel({
      id: 1,
      code: "PROJ1",
      description: "Project 1",
      assignments: [],
      assignmentStatistics: [],
    });
    projectServiceSpy.getProjectStatistics.and.returnValue(
      of(GetResult.success(stats)),
    );

    const oldDate = moment("2025-05-01T10:00:00Z");
    const newDate = moment("2025-05-02T10:00:00Z");
    component.courseId = 1;
    component.projectCode = "PROJ1";
    component.teamId = 0;
    component.statusDate = newDate;

    component.ngOnChanges({
      statusDate: new SimpleChange(oldDate, newDate, false),
    });

    expect(projectServiceSpy.getProjectStatistics).toHaveBeenCalledWith(
      1,
      "PROJ1",
      newDate,
    );
    expect(projectServiceSpy.getProjectSummary).not.toHaveBeenCalled();
  });

  it("should show toastr errors when loading statistics or summary fails", () => {
    const statsFail = new GetResult<TopicStatisticsModel>();
    statsFail.success = false;
    statsFail.message = "Stats error";
    projectServiceSpy.getProjectStatistics.and.returnValue(of(statsFail));

    const summaryFail = new GetResult<TopicSummaryModel>();
    summaryFail.success = false;
    summaryFail.message = "Summary error";
    projectServiceSpy.getProjectSummary.and.returnValue(of(summaryFail));

    component.courseId = 1;
    component.projectCode = "PROJ1";
    component.teamId = 10;

    component.ngOnChanges({
      projectCode: new SimpleChange("", "PROJ1", true),
    });

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Could not load project statistics from API. Message: Stats error",
      "System error",
    );
    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Could not load project summary from API. Message: Summary error",
      "System error",
    );
  });
});

