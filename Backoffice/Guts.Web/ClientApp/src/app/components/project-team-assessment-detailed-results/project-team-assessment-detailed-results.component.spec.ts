import {
  GradeCorrectionType,
  ProjectTeamAssessmentDetailedResultsComponent,
} from "./project-team-assessment-detailed-results.component";
import { ProjectService, ProjectTeamAssessmentService } from "src/app/services";
import { GetResult } from "src/app/util/result";
import { IAssessmentResultModel } from "src/app/viewmodels/projectassessment.model";
import { of } from "rxjs";
import {
  ActivatedRouteStub,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("ProjectTeamAssessmentDetailedResultsComponent", () => {
  let component: ProjectTeamAssessmentDetailedResultsComponent;
  let teamAssessmentServiceSpy: jasmine.SpyObj<ProjectTeamAssessmentService>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let toastrSpy: ReturnType<typeof createToastrSpy>;
  let routeStub: ActivatedRouteStub;

  const sampleResults: IAssessmentResultModel[] = [
    {
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
        selfValue: 5,
        averageSelfValue: 4,
        peerValue: 3.5,
        averagePeerValue: 3.25,
        score: null,
        selfScore: null,
        peerScore: null,
      },
      cooperationResult: {
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
      individualGrade: 0,
    },
  ];

  beforeEach(() => {
    teamAssessmentServiceSpy =
      jasmine.createSpyObj<ProjectTeamAssessmentService>(
        "ProjectTeamAssessmentService",
        ["getDetailedResultsOfProjectTeamAssessment"],
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

    component = new ProjectTeamAssessmentDetailedResultsComponent(
      teamAssessmentServiceSpy,
      projectServiceSpy,
      toastrSpy,
      routeStub as any,
    );
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("should load project details, set teams, and populate chart data on ngOnInit", () => {
    projectServiceSpy.getProjectDetails.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "PROJ1",
          description: "Project 1",
          assignments: [],
          components: [],
          teams: [
            { id: 10, name: "Team 1", members: [] },
            { id: 20, name: "Team 2", members: [] },
            { id: 30, name: "Team 3", members: [] },
          ],
        }),
      ),
    );
    teamAssessmentServiceSpy.getDetailedResultsOfProjectTeamAssessment.and.returnValue(
      of(GetResult.success(sampleResults)),
    );

    component.ngOnInit();

    expect(projectServiceSpy.getProjectDetails).toHaveBeenCalledWith(
      1,
      "PROJ1",
    );
    expect(component.currentTeam.id).toBe(20);
    expect(component.previousTeam?.id).toBe(10);
    expect(component.nextTeam?.id).toBe(30);
    expect(component.results.length).toBe(1);
    expect(component.peerAndSelfChartData.datasets.length).toBe(4);
    expect(component.selfChartData.datasets.length).toBe(4);
    expect(component.peerChartData.datasets.length).toBe(4);
    expect(component.peersVersusSelfChartData.datasets.length).toBe(2);
    expect(component.peersVersusSelfChartData.labels).toEqual(["Alice A"]);

    // Navigate to first team without reloading project details
    routeStub.setParams({ code: "PROJ1", assessmentId: 5, teamId: 10 });
    expect(projectServiceSpy.getProjectDetails).toHaveBeenCalledTimes(1);
    expect(component.currentTeam.id).toBe(10);
    expect(component.previousTeam).toBeNull();
    expect(component.nextTeam?.id).toBe(20);

    // Navigate to last team
    routeStub.setParams({ code: "PROJ1", assessmentId: 5, teamId: 30 });
    expect(component.currentTeam.id).toBe(30);
    expect(component.previousTeam?.id).toBe(20);
    expect(component.nextTeam).toBeNull();
  });

  it("should show toastr error when getProjectDetails fails and warning when getDetailedResults fails", () => {
    const projectFail = new GetResult<any>();
    projectFail.success = false;
    projectFail.message = "Project failed";
    projectServiceSpy.getProjectDetails.and.returnValue(of(projectFail));

    const resultsFail = new GetResult<any>();
    resultsFail.success = false;
    resultsFail.message = "No results yet";
    teamAssessmentServiceSpy.getDetailedResultsOfProjectTeamAssessment.and.returnValue(
      of(resultsFail),
    );

    component.ngOnInit();

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Could not load project from API. Message: Project failed",
      "System error",
    );
    expect(toastrSpy.warning).toHaveBeenCalledWith(
      "Could not retrieve project team assment results. Message: No results yet",
      "Warning",
    );
    expect(component.results).toEqual([]);
  });

  describe("calculateIndividualGrades", () => {
    beforeEach(() => {
      component.results = JSON.parse(JSON.stringify(sampleResults));
      component.teamGrade = 14;
    });

    it("should calculate individual grade based on Peers, Self, and PeersAndSelf and cap at 20", () => {
      component.gradeCorrectionType = GradeCorrectionType.Peers;
      component.calculateIndividualGrades();
      expect(component.results[0].individualGrade).toBe(
        +(14 * (3.5 / 3.25)).toFixed(2),
      );

      component.gradeCorrectionType = GradeCorrectionType.Self;
      component.calculateIndividualGrades();
      expect(component.results[0].individualGrade).toBe(
        +(14 * (5 / 4)).toFixed(2),
      );

      component.gradeCorrectionType = GradeCorrectionType.PeersAndSelf;
      component.calculateIndividualGrades();
      expect(component.results[0].individualGrade).toBe(
        +(14 * (4 / 3.5)).toFixed(2),
      );

      // Cap at 20
      component.teamGrade = 19;
      component.calculateIndividualGrades();
      expect(component.results[0].individualGrade).toBe(20);
    });
  });

  it("should format barChartOptions y-axis ticks and grid lineWidth", () => {
    const yScale: any = component.barChartOptions.scales.y;
    expect(yScale.ticks.callback(0)).toBe("No contribution (0)");
    expect(yScale.ticks.callback(1)).toBe("Way below average (1)");
    expect(yScale.ticks.callback(2)).toBe("Below average (2)");
    expect(yScale.ticks.callback(3)).toBe("Average (3)");
    expect(yScale.ticks.callback(4)).toBe("Above average (4)");
    expect(yScale.ticks.callback(5)).toBe("Way above average (5)");
    expect(yScale.ticks.callback(6)).toBe("");

    expect(yScale.grid.lineWidth({ tick: { value: 3 } }, {})).toBe(3);
    expect(yScale.grid.lineWidth({ tick: { value: 2 } }, {})).toBe(1);
  });
});
