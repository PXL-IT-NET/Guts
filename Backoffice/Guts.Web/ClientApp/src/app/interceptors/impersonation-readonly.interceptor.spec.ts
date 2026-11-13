import { TestBed } from "@angular/core/testing";
import { HttpClient, provideHttpClient, withInterceptors } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { signal } from "@angular/core";
import { impersonationReadOnlyInterceptor } from "./impersonation-readonly.interceptor";
import { AuthService } from "../services/auth.service";

describe("impersonationReadOnlyInterceptor", () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let isImpersonatingSignal: ReturnType<typeof signal<boolean>>;

  beforeEach(() => {
    isImpersonatingSignal = signal(false);
    const authServiceSpy = {
      isImpersonating: isImpersonatingSignal.asReadonly(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        provideHttpClient(withInterceptors([impersonationReadOnlyInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it("should allow GET requests when impersonating", () => {
    isImpersonatingSignal.set(true);

    http.get("/api/courses").subscribe();
    const req = httpMock.expectOne("/api/courses");
    expect(req.request.method).toBe("GET");
    req.flush([]);
  });

  it("should block POST requests with 403 when impersonating", () => {
    isImpersonatingSignal.set(true);
    let capturedStatus = 0;

    http.post("/api/courses", {}).subscribe({
      error: (err) => {
        capturedStatus = err.status;
      },
    });

    httpMock.expectNone("/api/courses");
    expect(capturedStatus).toBe(403);
  });
});
