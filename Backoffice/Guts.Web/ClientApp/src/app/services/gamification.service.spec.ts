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

  it("getProfile should fetch student profile", () => {
    let res: any;
    service.getProfile(10).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/profile")
      .flush({ userId: 1, courseId: 10, totalXp: 100 });

    expect(res.success).toBeTrue();
    expect(res.value.totalXp).toBe(100);
  });

  it("getProfile with periodId should append query param", () => {
    let res: any;
    service.getProfile(10, 5).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/profile?periodId=5")
      .flush({ userId: 1, courseId: 10, totalXp: 100 });

    expect(res.success).toBeTrue();
  });

  it("enroll should post enrollment data", () => {
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
  });

  it("getWeeklySprint should fetch weekly leaderboard", () => {
    let res: any;
    service.getWeeklySprint(10).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/weekly")
      .flush([{ rank: 1, userId: 5, score: 50 }]);

    expect(res.success).toBeTrue();
    expect(res.value).toHaveSize(1);
    expect(res.value[0].rank).toBe(1);
  });

  it("getClassLeaderboard should fetch class rankings", () => {
    let res: any;
    service.getClassLeaderboard(10).subscribe((r) => (res = r));

    httpMock
      .expectOne("api/courses/10/gamification/leaderboard/classes")
      .flush([{ rank: 1, classGroupName: "1TIN-A", averageXp: 85.0 }]);

    expect(res.success).toBeTrue();
    expect(res.value[0].classGroupName).toBe("1TIN-A");
  });

  it("getBadgeMetadata should fetch all badge metadata", () => {
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
  });
});
