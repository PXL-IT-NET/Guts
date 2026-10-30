import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { GamificationService } from "./gamification.service";
import { IEnrollStudentModel } from "../viewmodels/gamification.model";
import { PeriodProvider } from "./period.provider";

describe("GamificationService", () => {
  let service: GamificationService;
  let httpMock: HttpTestingController;
  let periodProvider: PeriodProvider;

  beforeEach(() => {
    periodProvider = new PeriodProvider();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PeriodProvider, useValue: periodProvider },
        GamificationService,
      ],
    });
    service = TestBed.inject(GamificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("getProfile should fetch student profile and handle error", () => {
    let res: any;
    service.getProfile(10).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/profile")
      .flush({ userId: 1, courseId: 10, totalXp: 100 });

    expect(res.success).toBeTrue();
    expect(res.value.totalXp).toBe(100);

    service.getProfile(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/profile")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("getProfile with periodId or periodProvider should append query param", () => {
    let res: any;
    service.getProfile(10, 5).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/profile?periodId=5")
      .flush({ userId: 1, courseId: 10, totalXp: 100 });

    expect(res.success).toBeTrue();
  });

  it("getProfile with perspectiveUserId should append perspectiveUserId query param", () => {
    let res: any;
    service.getProfile(10, 5, 42).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/profile?periodId=5&perspectiveUserId=42")
      .flush({ userId: 42, courseId: 10, totalXp: 200 });

    expect(res.success).toBeTrue();
    expect(res.value.userId).toBe(42);
  });

  it("enroll should post enrollment data and handle error", () => {
    let res: any;
    const dto: IEnrollStudentModel = {
      showInLeaderBoard: true,
      usePseudonym: true,
      pseudonym: "BitWizard",
      avatar: "Wizard",
      classGroupId: 2,
    };

    service.enroll(10, dto).subscribe((r) => (res = r));

    const req = httpMock.expectOne("api/courses/10/gamification/enroll");
    expect(req.request.method).toBe("POST");
    expect(req.request.body).toEqual(dto);

    req.flush({ userId: 1, courseId: 10, pseudonym: "BitWizard", showInLeaderBoard: true });
    expect(res.success).toBeTrue();
    expect(res.value.pseudonym).toBe("BitWizard");

    service.enroll(10, dto).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/enroll")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(res.success).toBeFalse();
  });

  it("getWeeklySprint and getHallOfFame should fetch leaderboards with perspective and tier params and handle errors", () => {
    let res: any;
    service.getWeeklySprint(10).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/weekly")
      .flush([{ rank: 1, userId: 5, score: 50 }]);

    expect(res.success).toBeTrue();
    expect(res.value).toHaveSize(1);

    service.getWeeklySprint(10, 2, 15).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/weekly?periodId=2&perspectiveUserId=15")
      .flush([{ rank: 1, userId: 15, score: 50, isCurrentUser: true }]);
    expect(res.success).toBeTrue();

    service.getWeeklySprint(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/weekly")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();

    const mockHallOfFameResponse = {
      entries: [{ rank: 1, userId: 5, score: 500 }],
      currentTier: {
        tier: 8,
        name: "Obsidian",
        icon: "🌋",
        minRank: 1,
        maxRank: 15,
        isCurrentTier: true,
      },
      visibleTiers: [],
      allTiers: [],
      nextTargetStudent: null,
      currentUserRank: 1,
      studentOptions: [],
    };

    service.getHallOfFame(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/hall-of-fame")
      .flush(mockHallOfFameResponse);
    expect(res.success).toBeTrue();
    expect(res.value.entries).toHaveSize(1);
    expect(res.value.currentTier?.name).toBe("Obsidian");

    service.getHallOfFame(10, 3, 25, 7).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/hall-of-fame?periodId=3&perspectiveUserId=25&tier=7")
      .flush(mockHallOfFameResponse);
    expect(res.success).toBeTrue();

    service.getHallOfFame(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/hall-of-fame")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("getClassLeaderboard should fetch class rankings and handle error", () => {
    let res: any;
    service.getClassLeaderboard(10).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/classes")
      .flush([{ rank: 1, classGroupName: "1TIN-A", averageXp: 85.0 }]);

    expect(res.success).toBeTrue();
    expect(res.value[0].classGroupName).toBe("1TIN-A");

    service.getClassLeaderboard(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/classes")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("getConfig and updateConfig should fetch and update config and handle errors", () => {
    let res: any;
    const cfg = {
      courseId: 10,
      isGamificationEnabled: true,
      isWeeklyStreakEnabled: true,
      isLeaderboardEnabled: true,
      isClassCompetitionEnabled: true,
      isTeamSynergyEnabled: true,
      isBadgesEnabled: true,
    };

    service.getConfig(10).subscribe((r) => (res = r));
    httpMock.expectOne("api/courses/10/gamification/config").flush(cfg);
    expect(res.success).toBeTrue();

    service.getConfig(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/config")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();

    service.updateConfig(10, cfg).subscribe((r) => (res = r));
    const putReq = httpMock.expectOne("api/courses/10/gamification/config");
    expect(putReq.request.method).toBe("PUT");
    putReq.flush(cfg);
    expect(res.success).toBeTrue();

    service.updateConfig(10, cfg).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/config")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("getClasses, addClass, and deleteClass should manage class groups and handle errors", () => {
    let res: any;
    service.getClasses(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/classes")
      .flush([{ id: 1, name: "1TIN-A", courseId: 10, periodId: 1 }]);
    expect(res.success).toBeTrue();

    service.getClasses(10).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/classes")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();

    service.addClass(10, "1TIN-B").subscribe((r) => (res = r));
    const postReq = httpMock.expectOne("api/courses/10/gamification/classes");
    expect(postReq.request.method).toBe("POST");
    postReq.flush({ id: 2, name: "1TIN-B", courseId: 10, periodId: 1 });
    expect(res.success).toBeTrue();

    service.addClass(10, "1TIN-B").subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/classes")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(res.success).toBeFalse();

    service.deleteClass(10, 2).subscribe((r) => (res = r));
    const delReq = httpMock.expectOne("api/courses/10/gamification/classes/2");
    expect(delReq.request.method).toBe("DELETE");
    delReq.flush({});
    expect(res.success).toBeTrue();

    service.deleteClass(10, 2).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/courses/10/gamification/classes/2")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });

  it("getBadgeMetadata should fetch all badge metadata and handle error", () => {
    let res: any;
    service.getBadgeMetadata().subscribe((r) => (res = r));

    httpMock
      .expectOne("api/gamification/badges")
      .flush([
        { badgeType: 1, type: 1, name: "First Spark", icon: "🌱", description: "Passed test" },
        { badgeType: 11, type: 11, name: "Sprint Gold", icon: "🥇", description: "Sprint #1" },
      ]);

    expect(res.success).toBeTrue();
    expect(res.value).toHaveSize(2);
    expect(res.value[0].name).toBe("First Spark");

    service.getBadgeMetadata().subscribe((r) => (res = r));
    httpMock
      .expectOne("api/gamification/badges")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });
});
