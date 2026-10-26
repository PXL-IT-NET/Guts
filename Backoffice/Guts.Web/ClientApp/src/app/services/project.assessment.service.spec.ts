import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
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
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ProjectAssessmentService,
      ],
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
        openOnUtc: new Date().toISOString(),
        deadlineUtc: new Date().toISOString(),
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
      openOnUtc: new Date().toISOString(),
      deadlineUtc: new Date().toISOString(),
    });
    expect(createRes.success).toBeTrue();

    service
      .addProjectAssessment(new ProjectAssessmentCreateModel(3))
      .subscribe((r) => (createRes = r));
    httpMock
      .expectOne("api/project-assessments")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(createRes.success).toBeFalse();

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

    service
      .updateProjectAssessment(updateModel)
      .subscribe((r) => (updateRes = r));
    httpMock
      .expectOne("api/project-assessments/2")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(updateRes.success).toBeFalse();

    let deleteRes: any;
    service.deleteProjectAssessment(2).subscribe((r) => (deleteRes = r));
    const delReq = httpMock.expectOne("api/project-assessments/2");
    expect(delReq.request.method).toBe("DELETE");
    delReq.flush({});
    expect(deleteRes.success).toBeTrue();

    service.deleteProjectAssessment(2).subscribe((r) => (deleteRes = r));
    httpMock
      .expectOne("api/project-assessments/2")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(deleteRes.success).toBeFalse();
  });
});
