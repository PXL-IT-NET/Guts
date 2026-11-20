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

  it("getAssignmentTimeline should build user/team timeline URLs and handle errors", () => {
    const date = new Date("2026-05-01T10:00:00Z");

    let userResult: any;
    service
      .getAssignmentTimeline(5, 12, 0, date)
      .subscribe((res) => (userResult = res));
    const userReq = httpMock.expectOne(
      `api/assignments/5/foruser/12/timeline?date=${encodeURIComponent(date.toISOString())}`,
    );
    expect(userReq.request.method).toBe("GET");
    userReq.flush({ assignmentId: 5, totalTestsCount: 3, runs: [] });
    expect(userResult.success).toBeTrue();
    expect(userResult.value.assignmentId).toBe(5);

    let teamResult: any;
    service
      .getAssignmentTimeline(5, 0, 8, "2026-05-01T10:00:00Z")
      .subscribe((res) => (teamResult = res));
    const teamReq = httpMock.expectOne(
      `api/assignments/5/forteam/8/timeline?date=${encodeURIComponent("2026-05-01T10:00:00Z")}`,
    );
    teamReq.flush("Server Error", { status: 500, statusText: "Server Error" });
    expect(teamResult.success).toBeFalse();
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

