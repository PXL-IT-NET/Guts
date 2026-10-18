import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { ChapterService } from "./chapter.service";
import { PeriodProvider } from "./period.provider";

describe("ChapterService", () => {
  let service: ChapterService;
  let httpMock: HttpTestingController;
  let periodProvider: PeriodProvider;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ChapterService,
        PeriodProvider,
      ],
    });
    service = TestBed.inject(ChapterService);
    httpMock = TestBed.inject(HttpTestingController);
    periodProvider = TestBed.inject(PeriodProvider);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("getChapterDetails should include periodId when set and handle success/error", () => {
    periodProvider.period = {
      id: 4,
      description: "25-26",
      from: null,
      until: null,
      isActive: true,
    };

    let result: any;
    service.getChapterDetails(1, "Ch1").subscribe((r) => (result = r));
    httpMock.expectOne("api/courses/1/chapters/Ch1?periodId=4").flush({
      id: 1,
      code: "Ch1",
    });
    expect(result.success).toBeTrue();

    periodProvider.period = null;
    service.getChapterDetails(1, "Ch1").subscribe((r) => (result = r));
    httpMock
      .expectOne("api/courses/1/chapters/Ch1")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(result.success).toBeFalse();
  });

  it("updateChapter should PUT chapter update and handle success/error", () => {
    let result: any;
    service
      .updateChapter(1, "Ch1", { description: "Updated" })
      .subscribe((r) => (result = r));

    const req = httpMock.expectOne("api/courses/1/chapters/Ch1");
    expect(req.request.method).toBe("PUT");
    req.flush({});
    expect(result.success).toBeTrue();

    service
      .updateChapter(1, "Ch1", { description: "Updated" })
      .subscribe((r) => (result = r));
    httpMock
      .expectOne("api/courses/1/chapters/Ch1")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(result.success).toBeFalse();
  });

  it("getChapterSummary and getChapterStatistics should construct query params with periodId and date", () => {
    const date = new Date("2026-06-01T12:00:00Z");
    periodProvider.period = {
      id: 2,
      description: "Period",
      from: null,
      until: null,
      isActive: true,
    };

    let summaryRes: any;
    service
      .getChapterSummary(1, "Ch1", 7, date)
      .subscribe((r) => (summaryRes = r));
    httpMock
      .expectOne(
        `api/courses/1/chapters/Ch1/users/7/summary?periodId=2&date=${date.toISOString()}`,
      )
      .flush({ id: 1, code: "Ch1", description: "D", assignmentSummaries: [] });
    expect(summaryRes.success).toBeTrue();

    periodProvider.period = null;
    let statsRes: any;
    service
      .getChapterStatistics(1, "Ch1", date)
      .subscribe((r) => (statsRes = r));
    httpMock
      .expectOne(
        `api/courses/1/chapters/Ch1/statistics?date=${date.toISOString()}`,
      )
      .flush({ id: 1, code: "Ch1", description: "D", assignmentStatistics: [] });
    expect(statsRes.success).toBeTrue();
  });
});

