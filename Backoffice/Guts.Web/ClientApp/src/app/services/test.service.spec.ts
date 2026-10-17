import {
  HttpClientTestingModule,
  HttpTestingController,
} from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { TestService } from "./test.service";

describe("TestService", () => {
  let service: TestService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TestService],
    });
    service = TestBed.inject(TestService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("deleteTest should send DELETE and handle success and error", () => {
    let res: any;
    service.deleteTest(11).subscribe((r) => (res = r));
    const req = httpMock.expectOne("api/tests/11");
    expect(req.request.method).toBe("DELETE");
    req.flush({});
    expect(res.success).toBeTrue();

    service.deleteTest(11).subscribe((r) => (res = r));
    httpMock
      .expectOne("api/tests/11")
      .flush("Err", { status: 400, statusText: "Bad Request" });
    expect(res.success).toBeFalse();
  });
});

