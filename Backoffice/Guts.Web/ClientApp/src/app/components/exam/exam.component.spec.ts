import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";

import { ExamComponent } from "./exam.component";
import { AuthService, ExamService } from "src/app/services";

describe("ExamComponent", () => {
  let component: ExamComponent;
  let fixture: ComponentFixture<ExamComponent>;

  beforeEach(() => {
    TestBed.overrideComponent(ExamComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [ExamComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({}),
            snapshot: { params: {} },
            parent: { snapshot: { params: {} } },
          },
        },
        {
          provide: ExamService,
          useValue: {
            getExamsOfCourse: () => of({ success: true, value: [] }),
            saveExam: () => of({ success: true, value: {} }),
            getExamResultsDownloadUrl: () => of({ success: true }),
          },
        },
        {
          provide: AuthService,
          useValue: {
            getUserProfile: () => of({}),
          },
        },
        {
          provide: ToastrService,
          useValue: { error: () => {}, success: () => {}, warning: () => {} },
        },
      ],
    });
    fixture = TestBed.createComponent(ExamComponent);
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
