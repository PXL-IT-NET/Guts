import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import moment from "moment";
import {
  ProjectAssessmentCreateModel,
  ProjectAssessmentModel,
  ProjectAssessmentUpdateModel,
} from "../viewmodels/projectassessment.model";
import { ProjectAssessmentService } from "./project.assessment.service";

describe("ProjectAssessmentService", () => {
  let service: ProjectAssessmentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ProjectAssessmentService],
    });
    service = TestBed.inject(ProjectAssessmentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("getAssessmentsOfProject should map ProjectAssessmentModel and handle error", () => {
    let res: any;
    service.getAssessmentsOfProject(3).subscribe((r) => (res = r));
    httpMock.expectOne("api/project-assessments/of-project/3").flush([
      {
        id: 1,
        description: "PA1",
        openOnUtc: moment.utc(),
        deadlineUtc: moment.utc(),
        teamStatus: null,
      },
    ]);
    expect(res.success).toBeTrue();
    expect(res.value[0] instanceof ProjectAssessmentModel).toBeTrue();

    service.getAssessmentsOfProject(3).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/project-assessments/of-project/3")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("addProjectAssessment, updateProjectAssessment, and deleteProjectAssessment should handle success and error", () => {
    let createRes: any;
    service
      .addProjectAssessment(new ProjectAssessmentCreateModel(3))
      .subscribe((r) => (createRes = r));
    const postReq = httpMock.expectOne("api/project-assessments");
    expect(postReq.request.method).toBe("POST");
    postReq.flush({
      id: 2,
      description: "PA2",
      openOnUtc: moment.utc(),
      deadlineUtc: moment.utc(),
    });
    expect(createRes.success).toBeTrue();

    const updateModel = new ProjectAssessmentUpdateModel();
    updateModel.id = 2;
    let updateRes: any;
    service
      .updateProjectAssessment(updateModel)
      .subscribe((r) => (updateRes = r));
    const putReq = httpMock.expectOne("api/project-assessments/2");
    expect(putReq.request.method).toBe("PUT");
    putReq.flush({});
    expect(updateRes.success).toBeTrue();

    let deleteRes: any;
    service.deleteProjectAssessment(2).subscribe((r) => (deleteRes = r));
    const delReq = httpMock.expectOne("api/project-assessments/2");
    expect(delReq.request.method).toBe("DELETE");
    delReq.flush({});
    expect(deleteRes.success).toBeTrue();
  });
});

