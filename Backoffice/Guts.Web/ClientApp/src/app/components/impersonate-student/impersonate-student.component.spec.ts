import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { ImpersonateStudentComponent } from "./impersonate-student.component";
import { ImpersonationService } from "../../services/impersonation.service";
import { AuthService } from "../../services/auth.service";
import { GetResult } from "../../util/result";
import { createToastrSpy } from "../../testing/test-helpers";

describe("ImpersonateStudentComponent", () => {
  let component: ImpersonateStudentComponent;
  let fixture: ComponentFixture<ImpersonateStudentComponent>;
  let impersonationServiceSpy: jasmine.SpyObj<ImpersonationService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    impersonationServiceSpy = jasmine.createSpyObj("ImpersonationService", [
      "searchUsers",
      "impersonate",
    ]);
    authServiceSpy = jasmine.createSpyObj("AuthService", ["startImpersonation"]);
    impersonationServiceSpy.searchUsers.and.returnValue(
      of(
        GetResult.success([
          {
            id: 5,
            email: "s@student.pxl.be",
            firstName: "Stu",
            lastName: "Dent",
            roles: ["student"],
          },
        ]),
      ),
    );
    toastrSpy = createToastrSpy();

    await TestBed.configureTestingModule({
      imports: [ImpersonateStudentComponent],
      providers: [
        { provide: ImpersonationService, useValue: impersonationServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ImpersonateStudentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should load impersonatable students on init", () => {
    expect(component.students().length).toBe(1);
    expect(component.students()[0].email).toBe("s@student.pxl.be");
  });
});
