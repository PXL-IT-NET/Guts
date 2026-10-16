import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import moment from "moment";
import { TeamGenerationModel } from "../viewmodels/team.model";
import { PeriodProvider } from "./period.provider";
import { ProjectService } from "./project.service";

describe("ProjectService", () => {
  let service: ProjectService;
  let httpMock: HttpTestingController;
  let periodProvider: PeriodProvider;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ProjectService, PeriodProvider],
    });
    service = TestBed.inject(ProjectService);
    httpMock = TestBed.inject(HttpTestingController);
    periodProvider = TestBed.inject(PeriodProvider);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("getProjectDetails, addProject, and updateProject should handle requests", () => {
    periodProvider.period = {
      id: 3,
      description: "P",
      from: null,
      until: null,
      isActive: true,
    };

    let detailRes: any;
    service.getProjectDetails(1, "P1").subscribe((r) => (detailRes = r));
    httpMock
      .expectOne("api/courses/1/projects/P1?periodId=3")
      .flush({ id: 1, code: "P1" });
    expect(detailRes.success).toBeTrue();

    let addRes: any;
    service
      .addProject(1, { code: "P2", description: "Project 2" })
      .subscribe((r) => (addRes = r));
    httpMock.expectOne("api/courses/1/projects").flush({ id: 2, code: "P2" });
    expect(addRes.success).toBeTrue();

    let updateRes: any;
    service
      .updateProject(1, "P2", { description: "Updated" })
      .subscribe((r) => (updateRes = r));
    const putReq = httpMock.expectOne("api/courses/1/projects/P2");
    expect(putReq.request.method).toBe("PUT");
    putReq.flush({});
    expect(updateRes.success).toBeTrue();
  });

  it("team management methods should call appropriate endpoints", () => {
    periodProvider.period = null;

    service.getTeams(1, "P1").subscribe();
    httpMock.expectOne("api/courses/1/projects/P1/teams").flush([]);

    service.getMyTeam(1, "P1").subscribe();
    httpMock
      .expectOne("api/courses/1/projects/P1/teams/my-team")
      .flush({ id: 5, name: "T1", members: [] });

    service.addTeam(1, "P1", "Team A").subscribe();
    httpMock
      .expectOne("api/courses/1/projects/P1/teams")
      .flush({ id: 6, name: "Team A", members: [] });

    service
      .updateTeam(1, "P1", { id: 6, name: "Team A+", members: [] })
      .subscribe();
    httpMock.expectOne("api/courses/1/projects/P1/teams/6").flush({});

    service
      .generateTeams(1, "P1", new TeamGenerationModel("Team", 1, 5))
      .subscribe();
    httpMock.expectOne("api/courses/1/projects/P1/teams/generate").flush({});

    service.deleteTeam(1, "P1", 6).subscribe();
    httpMock.expectOne("api/courses/1/projects/P1/teams/6").flush({});

    service.joinTeam(1, "P1", 6).subscribe();
    httpMock.expectOne("api/courses/1/projects/P1/teams/6/join").flush({});

    service.leaveTeam(1, "P1", 6).subscribe();
    httpMock.expectOne("api/courses/1/projects/P1/teams/6/leave").flush({});

    service.removeFromTeam(1, "P1", 6, 42).subscribe();
    const removeReq = httpMock.expectOne(
      "api/courses/1/projects/P1/teams/6/remove",
    );
    expect(removeReq.request.body).toBe(42);
    removeReq.flush({});
  });

  it("getProjectSummary and getProjectStatistics should append periodId and date", () => {
    const date = moment("2026-06-01T12:00:00Z");
    periodProvider.period = {
      id: 9,
      description: "P",
      from: null,
      until: null,
      isActive: true,
    };

    let summaryRes: any;
    service
      .getProjectSummary(1, "P1", 4, date)
      .subscribe((r) => (summaryRes = r));
    httpMock
      .expectOne(
        `api/courses/1/projects/P1/teams/4/summary?periodId=9&date=${date.toISOString()}`,
      )
      .flush({ id: 1, code: "P1", description: "D", assignmentSummaries: [] });
    expect(summaryRes.success).toBeTrue();

    let statsRes: any;
    service
      .getProjectStatistics(1, "P1", date)
      .subscribe((r) => (statsRes = r));
    httpMock
      .expectOne(
        `api/courses/1/projects/P1/statistics?periodId=9&date=${date.toISOString()}`,
      )
      .flush({ id: 1, code: "P1", description: "D", assignmentStatistics: [] });
    expect(statsRes.success).toBeTrue();
  });
});

