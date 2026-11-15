import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
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

  it("should getAll periods and handle error", () => {
    service.getAll().subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.length).toBe(1);
    });
    httpMock.expectOne("api/periods").flush([
      {
        id: 1,
        description: "2026-2027",
        from: "2026-09-01",
        until: "2027-08-31",
        isActive: true,
      },
    ]);

    service.getAll().subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/periods")
      .flush("Err", { status: 500, statusText: "Err" });
  });

  it("should create, update, delete period and handle errors", () => {
    const payload = {
      description: "2027-2028",
      from: "2027-09-01",
      until: "2028-08-31",
    };

    service.createPeriod(payload).subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.id).toBe(2);
    });
    httpMock
      .expectOne((r) => r.method === "POST" && r.url === "api/periods")
      .flush({ id: 2, ...payload, isActive: false });

    service
      .createPeriod(payload)
      .subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/periods")
      .flush("Bad", { status: 400, statusText: "Bad" });

    service
      .updatePeriod(2, payload)
      .subscribe((res) => expect(res.success).toBeTrue());
    httpMock
      .expectOne((r) => r.method === "PUT" && r.url === "api/periods/2")
      .flush(null);

    service
      .updatePeriod(2, payload)
      .subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/periods/2")
      .flush("Bad", { status: 400, statusText: "Bad" });

    service.deletePeriod(2).subscribe((res) => expect(res.success).toBeTrue());
    httpMock
      .expectOne((r) => r.method === "DELETE" && r.url === "api/periods/2")
      .flush(null);

    service.deletePeriod(2).subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/periods/2")
      .flush("Err", { status: 404, statusText: "Not Found" });
  });
});
