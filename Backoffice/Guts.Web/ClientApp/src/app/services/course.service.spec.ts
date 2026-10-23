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

  it("getCourses should fetch courses and handle error", () => {
    let res: any;
    service.getCourses().subscribe((r) => (res = r));
    httpMock.expectOne("api/courses").flush([{ id: 1, code: "C1", name: "N" }]);
    expect(res.success).toBeTrue();
    expect(res.value.length).toBe(1);

    service.getCourses().subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("getCourseContentsById should optionally append periodId and handle error", () => {
    let res: any;
    service.getCourseContentsById(5).subscribe((r) => (res = r));
    httpMock.expectOne("api/courses/5").flush({ id: 5 });
    expect(res.success).toBeTrue();

    service.getCourseContentsById(5, 3).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/5?periodId=3")
      .flush("Err", { status: 404, statusText: "Not Found" });
    expect(res.success).toBeFalse();
  });

  it("getCourseStatistics should optionally append periodId and handle error", () => {
    let res: any;
    service.getCourseStatistics(10).subscribe((r) => (res = r));
    httpMock.expectOne("api/courses/10/statistics").flush({ courseId: 10, students: [] });
    expect(res.success).toBeTrue();
    expect(res.value.courseId).toBe(10);

    service.getCourseStatistics(10, 2).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/statistics?periodId=2")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });
});
