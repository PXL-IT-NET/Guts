import { TestBed } from "@angular/core/testing";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { provideHttpClient } from "@angular/common/http";
import {
  IQuartzDashboardTicketModel,
  ScheduledJobsService,
} from "./scheduled-jobs.service";

describe("ScheduledJobsService", () => {
  let service: ScheduledJobsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ScheduledJobsService,
      ],
    });

    service = TestBed.inject(ScheduledJobsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should request a dashboard ticket and return loginUrl on success", () => {
    const mockResponse: IQuartzDashboardTicketModel = {
      loginUrl: "https://guts-api.pxl.be/api/scheduled-jobs/dashboard-login?ticket=abc",
    };

    service.createDashboardTicket().subscribe((result) => {
      expect(result.success).toBeTrue();
      expect(result.value).toEqual(mockResponse);
    });

    const req = httpMock.expectOne("api/scheduled-jobs/dashboard-ticket");
    expect(req.request.method).toBe("POST");
    req.flush(mockResponse);
  });

  it("should return failure GetResult on HTTP error", () => {
    service.createDashboardTicket().subscribe((result) => {
      expect(result.success).toBeFalse();
      expect(result.message).toBeTruthy();
    });

    const req = httpMock.expectOne("api/scheduled-jobs/dashboard-ticket");
    expect(req.request.method).toBe("POST");
    req.flush("Forbidden", { status: 403, statusText: "Forbidden" });
  });
});
