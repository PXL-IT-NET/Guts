import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import {
  PeerAssessmentModel,
  ProjectTeamAssessmentStatusModel,
} from "../viewmodels/projectassessment.model";
import { ProjectTeamAssessmentService } from "./project.teamassessment.service";

describe("ProjectTeamAssessmentService", () => {
  let service: ProjectTeamAssessmentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ProjectTeamAssessmentService],
    });
    service = TestBed.inject(ProjectTeamAssessmentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should fetch status, detailed results, my result, peer assessments, and save peer assessments", () => {
    let statusRes: any;
    service
      .getStatusOfProjectTeamAssessment(10, 20)
      .subscribe((r) => (statusRes = r));
    httpMock
      .expectOne(
        "api/project-team-assessments/of-project-assessment/10/of-team/20/status",
      )
      .flush({
        id: 1,
        teamId: 20,
        isComplete: true,
        peersThatNeedToEvaluateOthers: [],
      });
    expect(statusRes.success).toBeTrue();
    expect(
      statusRes.value instanceof ProjectTeamAssessmentStatusModel,
    ).toBeTrue();

    let detailedRes: any;
    service
      .getDetailedResultsOfProjectTeamAssessment(10, 20)
      .subscribe((r) => (detailedRes = r));
    httpMock
      .expectOne(
        "api/project-team-assessments/of-project-assessment/10/of-team/20/detailed-results",
      )
      .flush([]);
    expect(detailedRes.success).toBeTrue();

    let myRes: any;
    service
      .getMyResultOfProjectTeamAssessment(10, 20)
      .subscribe((r) => (myRes = r));
    httpMock
      .expectOne(
        "api/project-team-assessments/of-project-assessment/10/of-team/20/my-result",
      )
      .flush({ individualGrade: 15 });
    expect(myRes.success).toBeTrue();

    let peersRes: any;
    service.getPeerAssessmentsOfUser(10, 20).subscribe((r) => (peersRes = r));
    httpMock
      .expectOne(
        "api/project-team-assessments/of-project-assessment/10/of-team/20/peer-assessments",
      )
      .flush([
        {
          subject: { id: 1, fullName: "A" },
          user: { id: 1, fullName: "A" },
          contributionScore: 3,
          cooperationScore: 3,
          effortScore: 3,
          isSelfAssessment: true,
          explanation: "",
        },
      ]);
    expect(peersRes.success).toBeTrue();
    expect(peersRes.value[0] instanceof PeerAssessmentModel).toBeTrue();

    let saveRes: any;
    service
      .savePeerAssessment(10, 20, [new PeerAssessmentModel()])
      .subscribe((r) => (saveRes = r));
    const postReq = httpMock.expectOne(
      "api/project-team-assessments/of-project-assessment/10/of-team/20/peer-assessments",
    );
    expect(postReq.request.method).toBe("POST");
    postReq.flush({});
    expect(saveRes.success).toBeTrue();
  });
});

