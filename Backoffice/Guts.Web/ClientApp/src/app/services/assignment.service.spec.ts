import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { AssignmentService } from "./assignment.service";

describe("AssignmentService", () => {
  let service: AssignmentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AssignmentService,
      ],
    });
    service = TestBed.inject(AssignmentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("getAssignmentDetail should build URL for user, team, and optional date", () => {
    const date = new Date("2026-05-01T10:00:00Z");

    service.getAssignmentDetail(5, 12, 0, date).subscribe();
    const userReq = httpMock.expectOne(
      `api/assignments/5/foruser/12?date=${date.toISOString()}`,
    );
    expect(userReq.request.method).toBe("GET");
    userReq.flush({});

    service.getAssignmentDetail(5, 0, 8).subscribe();
    const teamReq = httpMock.expectOne("api/assignments/5/forteam/8");
    expect(teamReq.request.method).toBe("GET");
    teamReq.flush({});
  });

  it("getAssignmentsOfCourse should handle success and error", () => {
    let successResult: any;
    service
      .getAssignmentsOfCourse(3)
      .subscribe((res) => (successResult = res));

    httpMock.expectOne("api/assignments/ofcourse/3").flush([]);
    expect(successResult.success).toBeTrue();
    expect(successResult.value).toEqual([]);

    let errorResult: any;
    service.getAssignmentsOfCourse(3).subscribe((res) => (errorResult = res));
    httpMock
      .expectOne("api/assignments/ofcourse/3")
      .flush("Failed", { status: 500, statusText: "Server Error" });
    expect(errorResult.success).toBeFalse();
  });

  it("deleteAssignment should send DELETE and handle success and error", () => {
    let successResult: any;
    service.deleteAssignment(9).subscribe((res) => (successResult = res));

    const req = httpMock.expectOne("api/assignments/9");
    expect(req.request.method).toBe("DELETE");
    req.flush({});
    expect(successResult.success).toBeTrue();

    let errorResult: any;
    service.deleteAssignment(9).subscribe((res) => (errorResult = res));
    httpMock
      .expectOne("api/assignments/9")
      .flush({ error: "Cannot delete" }, { status: 400, statusText: "Bad Request" });
    expect(errorResult.success).toBeFalse();
  });
});

