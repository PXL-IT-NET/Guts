import { provideHttpClient } from "@angular/common/http";
import {
  HttpTestingController,
  provideHttpClientTesting,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { AdminUserService } from "./admin-user.service";

describe("AdminUserService", () => {
  let service: AdminUserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AdminUserService,
      ],
    });
    service = TestBed.inject(AdminUserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should searchUsers with default and filter query parameters", () => {
    service.searchUsers().subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.totalCount).toBe(0);
    });
    const defaultReq = httpMock.expectOne(
      (r) =>
        r.url === "api/admin/users" &&
        r.params.get("page") === "1" &&
        r.params.get("pageSize") === "25" &&
        !r.params.has("role") &&
        !r.params.has("orphanedOnly") &&
        !r.params.has("lockedOnly"),
    );
    defaultReq.flush({ items: [], totalCount: 0, page: 1, pageSize: 25 });

    service
      .searchUsers("ada", "lector", true, 2, null, true)
      .subscribe((res) => {
        expect(res.success).toBeTrue();
      });
    const filteredReq = httpMock.expectOne(
      (r) =>
        r.url === "api/admin/users" &&
        r.params.get("search") === "ada" &&
        r.params.get("role") === "lector" &&
        r.params.get("orphanedOnly") === "true" &&
        r.params.get("lockedOnly") === "true" &&
        r.params.get("page") === "2" &&
        !r.params.has("pageSize"),
    );
    filteredReq.flush({ items: [], totalCount: 0, page: 2, pageSize: null });
  });

  it("should map HTTP error on searchUsers", () => {
    service.searchUsers("fail").subscribe((res) => {
      expect(res.success).toBeFalse();
    });
    const req = httpMock.expectOne((r) => r.url === "api/admin/users");
    req.flush("Server Error", { status: 500, statusText: "Server Error" });
  });

  it("should deleteUser and handle error", () => {
    service.deleteUser(5).subscribe((res) => expect(res.success).toBeTrue());
    httpMock
      .expectOne((r) => r.method === "DELETE" && r.url === "api/admin/users/5")
      .flush(null);

    service.deleteUser(6).subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne((r) => r.method === "DELETE" && r.url === "api/admin/users/6")
      .flush("Bad", { status: 400, statusText: "Bad Request" });
  });

  it("should deleteUsers batch and handle error", () => {
    service.deleteUsers([1, 2]).subscribe((res) => {
      expect(res.success).toBeTrue();
      expect(res.value.deletedCount).toBe(2);
    });
    const req = httpMock.expectOne(
      (r) =>
        r.method === "POST" && r.url === "api/admin/users/delete-batch",
    );
    expect(req.request.body).toEqual({ userIds: [1, 2] });
    req.flush({ deletedCount: 2 });

    service.deleteUsers([1]).subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/admin/users/delete-batch")
      .flush("Err", { status: 400, statusText: "Bad Request" });
  });

  it("should promoteToAdmin, demoteFromAdmin, unlockLector, and handle errors", () => {
    service
      .promoteToAdmin(10)
      .subscribe((res) => expect(res.success).toBeTrue());
    httpMock
      .expectOne("api/admin/users/10/promote-admin")
      .flush(null);

    service
      .demoteFromAdmin(10)
      .subscribe((res) => expect(res.success).toBeTrue());
    httpMock
      .expectOne("api/admin/users/10/demote-admin")
      .flush(null);

    service.unlockLector(10).subscribe((res) => expect(res.success).toBeTrue());
    httpMock.expectOne("api/admin/users/10/unlock").flush(null);

    service
      .unlockLector(11)
      .subscribe((res) => expect(res.success).toBeFalse());
    httpMock
      .expectOne("api/admin/users/11/unlock")
      .flush("Fail", { status: 400, statusText: "Bad Request" });
  });
});
