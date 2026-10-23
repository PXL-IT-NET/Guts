import { TestBed } from "@angular/core/testing";
import { ProjectTeamAssessmentMyResultComponent } from "./project-team-assessment-my-result.component";
import { ProjectService, ProjectTeamAssessmentService } from "src/app/services";
import { GetResult } from "src/app/util/result";
import { IAssessmentResultModel } from "src/app/viewmodels/projectassessment.model";
import { of } from "rxjs";
import {
  ActivatedRouteStub,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("ProjectTeamAssessmentMyResultComponent", () => {
  let component: ProjectTeamAssessmentMyResultComponent;
  let teamAssessmentServiceSpy: jasmine.SpyObj<ProjectTeamAssessmentService>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let toastrSpy: ReturnType<typeof createToastrSpy>;
  let routeStub: ActivatedRouteStub;

  const sampleResult: IAssessmentResultModel = {
    subject: { id: 1, fullName: "Alice A" },
    selfAssessment: null,
    peerAssessments: [],
    averageResult: {
      value: 4,
      averageValue: 3.5,
      selfValue: 5,
      averageSelfValue: 4,
      peerValue: 3.5,
      averagePeerValue: 3.25,
      score: null,
      selfScore: null,
      peerScore: null,
    },
    effortResult: {
      value: 4,
      averageValue: 3.5,
      selfValue: 4,
      averageSelfValue: 4,
      peerValue: 3,
      averagePeerValue: 3,
      score: null,
      selfScore: null,
      peerScore: null,
    },
    cooperationResult: {
      value: 4,
      averageValue: 3.5,
      selfValue: 5,
      averageSelfValue: 4,
      peerValue: 4,
      averagePeerValue: 3.5,
      score: null,
      selfScore: null,
      peerScore: null,
    },
    contributionResult: {
      value: 4,
      averageValue: 3.5,
      selfValue: 5,
      averageSelfValue: 4,
      peerValue: 3.5,
      averagePeerValue: 3.25,
      score: null,
      selfScore: null,
      peerScore: null,
    },
    individualGrade: 15,
  };

  beforeEach(() => {
    teamAssessmentServiceSpy =
      jasmine.createSpyObj<ProjectTeamAssessmentService>(
        "ProjectTeamAssessmentService",
        ["getMyResultOfProjectTeamAssessment"],
      );
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "getProjectDetails",
    ]);
    toastrSpy = createToastrSpy();

    const parentRoute = new ActivatedRouteStub({ courseId: "1" });
    routeStub = new ActivatedRouteStub(
      { code: "PROJ1", assessmentId: 5, teamId: 20 },
      {},
      parentRoute,
    );

    component = TestBed.runInInjectionContext(() => new ProjectTeamAssessmentMyResultComponent(
      teamAssessmentServiceSpy,
      toastrSpy,
      routeStub as any,
    ));
  });

  it("should load my result and populate radar chart data on ngOnInit", () => {
    teamAssessmentServiceSpy.getMyResultOfProjectTeamAssessment.and.returnValue(
      of(GetResult.success(sampleResult)),
    );

    component.ngOnInit();

    expect(
      teamAssessmentServiceSpy.getMyResultOfProjectTeamAssessment,
    ).toHaveBeenCalledWith(5, 20);
    expect(component.result).toEqual(sampleResult);
    expect(component.chartData.datasets.length).toBe(2);
    expect(component.chartData.datasets[0].label).toBe("Peers");
    expect(component.chartData.datasets[0].data).toEqual([3.5, 3, 3.5, 4]);
    expect(component.chartData.datasets[1].label).toBe("Self");
    expect(component.chartData.datasets[1].data).toEqual([5, 4, 5, 5]);
    expect(component.chartData.labels).toEqual([
      "Average",
      "Effort",
      "Contribution",
      "Cooperation",
    ]);
  });

  it("should show toastr warning when getMyResultOfProjectTeamAssessment fails", () => {
    const failResult = new GetResult<IAssessmentResultModel>();
    failResult.success = false;
    failResult.message = "Not completed yet";
    teamAssessmentServiceSpy.getMyResultOfProjectTeamAssessment.and.returnValue(
      of(failResult),
    );

    component.ngOnInit();

    expect(toastrSpy.warning).toHaveBeenCalledWith(
      "Could not retrieve project team assment result. Message: Not completed yet",
      "Warning",
    );
    expect(component.result).toBeNull();
  });
});

