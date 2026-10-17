import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { ExamModel, ExamPartModel } from "../viewmodels/exam.model";
import { ExamService } from "./exam.service";

describe("ExamService", () => {
  let service: ExamService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ExamService],
    });
    service = TestBed.inject(ExamService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("getExamsOfCourse should map exams and handle error", () => {
    let res: any;
    service.getExamsOfCourse(2).subscribe((r) => (res = r));
    httpMock.expectOne("api/exams?courseId=2").flush([
      {
        id: 1,
        courseId: 2,
        name: "Exam 1",
        maximumScore: 20,
        parts: [],
      },
    ]);
    expect(res.success).toBeTrue();
    expect(res.value[0] instanceof ExamModel).toBeTrue();

    service.getExamsOfCourse(2).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/exams?courseId=2")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("saveExam and saveExamPart should POST and map results or handle error", () => {
    let examRes: any;
    const exam = new ExamModel();
    service.saveExam(exam).subscribe((r) => (examRes = r));
    const req1 = httpMock.expectOne("api/exams");
    expect(req1.request.method).toBe("POST");
    req1.flush({ id: 5, courseId: 2, name: "New", maximumScore: 20, parts: [] });
    expect(examRes.success).toBeTrue();

    let partRes: any;
    const part = new ExamPartModel();
    service.saveExamPart(5, part).subscribe((r) => (partRes = r));
    const req2 = httpMock.expectOne("api/exams/5/parts");
    expect(req2.request.method).toBe("POST");
    req2.flush({
      id: 10,
      name: "P1",
      deadline: new Date(),
      assignmentEvaluations: [],
    });
    expect(partRes.success).toBeTrue();
  });

  it("deleteExamPart should DELETE exam part and handle error", () => {
    let res: any;
    service.deleteExamPart(5, 10).subscribe((r) => (res = r));
    const req = httpMock.expectOne("api/exams/5/parts/10");
    expect(req.request.method).toBe("DELETE");
    req.flush({});
    expect(res.success).toBeTrue();

    service.deleteExamPart(5, 10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/exams/5/parts/10")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(res.success).toBeFalse();
  });

  it("getExamResultsDownloadUrl should handle HTTP error", () => {
    let res: any;
    service.getExamResultsDownloadUrl(5).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/exams/5/downloadscores")
      .flush(new Blob(), { status: 500, statusText: "Server Error" });
    expect(res.success).toBeFalse();
  });
});

