import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { ImpersonationService } from "./impersonation.service";

describe("ImpersonationService", () => {
  let service: ImpersonationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ImpersonationService,
      ],
    });
    service = TestBed.inject(ImpersonationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should searchUsers with params and handle HTTP error", () => {
    service.searchUsers().subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.totalCount).toBe(1);
    });
    const req1 = httpMock.expectOne(
      (r) =>
        r.url === "api/impersonation/users" &&
        r.params.get("page") === "1" &&
        r.params.get("pageSize") === "25",
    );
    req1.flush({ items: [{ id: 1 }], totalCount: 1, page: 1, pageSize: 25 });

    service.searchUsers("bob", 2, null).subscribe((res) => {
      expect(res.success).toBeFalse();
    });
    const req2 = httpMock.expectOne(
      (r) =>
        r.url === "api/impersonation/users" &&
        r.params.get("search") === "bob" &&
        !r.params.has("pageSize"),
    );
    req2.flush("Fail", { status: 500, statusText: "Server Error" });
  });

  it("should impersonate user and handle HTTP error", () => {
    service.impersonate(42).subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.token).toBe("imp-jwt");
    });
    const req1 = httpMock.expectOne(
      (r) => r.method === "POST" && r.url === "api/impersonation/42",
    );
    req1.flush({ token: "imp-jwt", expiration: "2026-12-01" });

    service.impersonate(99).subscribe((res) => {
      expect(res.success).toBeFalse();
    });
    httpMock
      .expectOne("api/impersonation/99")
      .flush("Forbidden", { status: 403, statusText: "Forbidden" });
  });
});
