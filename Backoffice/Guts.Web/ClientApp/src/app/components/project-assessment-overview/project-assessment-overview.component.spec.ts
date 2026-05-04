import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { BsModalService } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { UntypedFormBuilder } from "@angular/forms";

import { ProjectAssessmentOverviewComponent } from "./project-assessment-overview.component";
import {
  AuthService,
  PeriodProvider,
  ProjectService,
  ProjectTeamAssessmentService,
} from "src/app/services";
import { ProjectAssessmentService } from "src/app/services/project.assessment.service";

describe("ProjectAssessmentOverviewComponent", () => {
  let component: ProjectAssessmentOverviewComponent;
  let fixture: ComponentFixture<ProjectAssessmentOverviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ProjectAssessmentOverviewComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        UntypedFormBuilder,
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({}),
            queryParams: of({}),
            snapshot: { params: {} },
            parent: { snapshot: { params: {} } },
          },
        },
        {
          provide: ProjectService,
          useValue: {
            getProjectDetails: () => of({ success: false, message: "" }),
          },
        },
        {
          provide: ProjectAssessmentService,
          useValue: {
            getAssessmentsOfProject: () => of({ success: true, value: [] }),
          },
        },
        {
          provide: ProjectTeamAssessmentService,
          useValue: {
            getStatusOfProjectTeamAssessment: () =>
              of({ success: true, value: null }),
          },
        },
        {
          provide: BsModalService,
          useValue: {
            show: () => ({
              setClass: () => {},
              content: {
                assessmentAdded: of(null),
                assessmentEdited: of(null),
              },
            }),
          },
        },
        {
          provide: AuthService,
          useValue: {
            getUserProfile: () =>
              of({ isLector: () => false, isStudent: () => true }),
          },
        },
        {
          provide: ToastrService,
          useValue: { error: () => {}, success: () => {}, warning: () => {} },
        },
        {
          provide: PeriodProvider,
          useValue: { period$: of({ isActive: true }) },
        },
      ],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ProjectAssessmentOverviewComponent);
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
