import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { CourseService } from "./course.service";

describe("CourseService", () => {
  let service: CourseService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        CourseService,
      ],
    });
    service = TestBed.inject(CourseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should get courses, contents, and statistics and handle errors", () => {
    service.getCourses().subscribe((res) => expect(res.success).toBeTrue());
    httpMock.expectOne("api/courses").flush([]);

    service.getCourses().subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/courses")
      .flush("Err", { status: 500, statusText: "Err" });

    service
      .getCourseContentsById(5, 2)
      .subscribe((res) => expect(res.success).toBeTrue());
    httpMock.expectOne("api/courses/5?periodId=2").flush({});

    service
      .getCourseContentsById(5)
      .subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/courses/5")
      .flush("Err", { status: 404, statusText: "Not Found" });

    service
      .getCourseStatistics(5, 2)
      .subscribe((res) => expect(res.success).toBeTrue());
    httpMock.expectOne("api/courses/5/statistics?periodId=2").flush({});

    service
      .getCourseStatistics(5)
      .subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/courses/5/statistics")
      .flush("Err", { status: 500, statusText: "Err" });
  });

  it("should create, update, delete course and handle errors", () => {
    const model = { code: "CS101", name: "Intro" };

    service.createCourse(model).subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.id).toBe(10);
    });
    httpMock
      .expectOne((r) => r.method === "POST" && r.url === "api/courses")
      .flush({ id: 10, ...model });

    service
      .createCourse(model)
      .subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/courses")
      .flush("Bad", { status: 400, statusText: "Bad" });

    service.updateCourse(10, model).subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.name).toBe("Intro");
    });
    httpMock
      .expectOne((r) => r.method === "PUT" && r.url === "api/courses/10")
      .flush({ id: 10, ...model });

    service
      .updateCourse(10, model)
      .subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/courses/10")
      .flush("Bad", { status: 400, statusText: "Bad" });

    service.deleteCourse(10).subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.jobId).toBe("job-1");
      expect(res.value.status).toBe("Pending");
    });
    httpMock
      .expectOne((r) => r.method === "DELETE" && r.url === "api/courses/10")
      .flush(
        {
          jobId: "job-1",
          targetType: "Course",
          targetId: 10,
          targetLabel: "CS101 - Intro",
          status: "Pending",
          totalRows: 50,
          deletedRows: 0,
          percentage: 0,
          currentPhase: "Queued",
          createdAtUtc: "2026-11-15T12:00:00Z",
        },
        { status: 202, statusText: "Accepted" },
      );

    service.deleteCourse(10).subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/courses/10")
      .flush("NotFound", { status: 404, statusText: "Not Found" });
  });
});
