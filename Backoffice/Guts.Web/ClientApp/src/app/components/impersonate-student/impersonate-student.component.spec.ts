import { ComponentFixture, TestBed } from "@angular/core/testing";
import { HttpErrorResponse } from "@angular/common/http";
import { of } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { ImpersonateStudentComponent } from "./impersonate-student.component";
import {
  IImpersonatableUser,
  ImpersonationService,
} from "../../services/impersonation.service";
import { AuthService } from "../../services/auth.service";
import { GetResult } from "../../util/result";
import { IPagedResult } from "../../util/paged-result";
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
        GetResult.success({
          items: [
            {
              id: 5,
              email: "s@student.pxl.be",
              firstName: "Stu",
              lastName: "Dent",
              roles: ["student"],
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 25,
        }),
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

  it("should load impersonatable students on init with default paging", () => {
    expect(impersonationServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      1,
      25,
    );
    expect(component.students()).toHaveSize(1);
    expect(component.students()[0].email).toBe("s@student.pxl.be");
  });

  it("should change page size including All and trigger search", () => {
    component.onPageSizeChange(100);
    expect(impersonationServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      1,
      100,
    );

    component.onPageSizeChange(null);
    expect(impersonationServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      1,
      null,
    );
  });

  it("should navigate pages via goToPage", () => {
    impersonationServiceSpy.searchUsers.and.returnValue(
      of(
        GetResult.success({
          items: [
            {
              id: 5,
              email: "s@student.pxl.be",
              firstName: "Stu",
              lastName: "Dent",
              roles: ["student"],
            },
          ],
          totalCount: 60,
          page: 1,
          pageSize: 25,
        }),
      ),
    );
    component.loadUsers();
    fixture.detectChanges();

    expect(component.totalPages()).toBe(3);

    component.goToPage(2);
    expect(impersonationServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      2,
      25,
    );
  });

  it("should reset page to 1 on search and clear search term", () => {
    component.currentPage.set(3);
    component.searchTerm = "stu";
    component.search();
    expect(impersonationServiceSpy.searchUsers).toHaveBeenCalledWith(
      "stu",
      1,
      25,
    );

    component.clearSearchTerm();
    expect(component.searchTerm).toBe("");
    expect(impersonationServiceSpy.searchUsers).toHaveBeenCalledWith(
      "",
      1,
      25,
    );
  });

  it("should show toastr error when searchUsers fails", () => {
    impersonationServiceSpy.searchUsers.and.returnValue(
      of(
        GetResult.fromHttpErrorResponse<IPagedResult<IImpersonatableUser>>(
          new HttpErrorResponse({
            status: 500,
            error: "Server error",
          }),
        ),
      ),
    );

    component.loadUsers();
    expect(toastrSpy.error).toHaveBeenCalled();
  });

  it("should render centered pagination and items-per-page selector in the pagination footer", () => {
    const footer = fixture.nativeElement.querySelector(".pagination-footer");
    expect(footer).toBeTruthy();
    expect(
      footer.querySelector(
        "[data-testid='impersonate-pagination'] ul.justify-content-center",
      ),
    ).toBeTruthy();
    expect(
      footer.querySelector("[data-testid='impersonate-page-size-select']"),
    ).toBeTruthy();
  });
});
