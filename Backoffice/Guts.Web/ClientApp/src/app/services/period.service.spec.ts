import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { PeriodModel } from "../viewmodels/period.model";
import { PeriodService } from "./period.service";

describe("PeriodService", () => {
  let service: PeriodService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PeriodService,
      ],
    });
    service = TestBed.inject(PeriodService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("getAll should map PeriodModels and handle error", () => {
    let res: any;
    service.getAll().subscribe((r) => (res = r));
    httpMock.expectOne("api/periods").flush([
      {
        id: 1,
        description: "25-26",
        from: "2025-09-01",
        until: "2026-08-31",
        isActive: true,
      },
    ]);
    expect(res.success).toBeTrue();
    expect(res.value[0] instanceof PeriodModel).toBeTrue();

    service.getAll().subscribe((r) => (res = r));
    httpMock
      .expectOne("api/periods")
      .flush("Err", { status: 500, statusText: "Error" });
    expect(res.success).toBeFalse();
  });
});

