import { waitForAsync, ComponentFixture, TestBed } from "@angular/core/testing";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";

import { ExampartComponent } from "./exampart.component";
import { AssignmentService, ExamService } from "src/app/services";

describe("ExampartComponent", () => {
  let component: ExampartComponent;
  let fixture: ComponentFixture<ExampartComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.overrideComponent(ExampartComponent, { set: { template: "" } });
    TestBed.configureTestingModule({
      declarations: [ExampartComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
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
          provide: AssignmentService,
          useValue: {
            getAssignmentsOfCourse: () => of({ success: true, value: [] }),
          },
        },
        {
          provide: ExamService,
          useValue: {
            saveExamPart: () => of({ success: true, value: {} }),
            deleteExamPart: () => of({ success: true }),
          },
        },
        {
          provide: ToastrService,
          useValue: { error: () => {}, success: () => {}, warning: () => {} },
        },
      ],
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ExampartComponent);
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
