import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ActivatedRoute, Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";

import { ProjectTeamAssessmentEvaluationFormComponent } from "./project-team-assessment-evaluation-form.component";
import { ProjectTeamAssessmentService } from "src/app/services";

describe("ProjectTeamAssessmentEvaluationFormComponent", () => {
  let component: ProjectTeamAssessmentEvaluationFormComponent;
  let fixture: ComponentFixture<ProjectTeamAssessmentEvaluationFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ProjectTeamAssessmentEvaluationFormComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            params: of({}),
            snapshot: { params: {} },
            parent: { snapshot: { params: {} } },
          },
        },
        { provide: Router, useValue: { navigate: () => {} } },
        {
          provide: ProjectTeamAssessmentService,
          useValue: {
            getPeerAssessmentsOfUser: () => of({ success: true, value: [] }),
          },
        },
        {
          provide: ToastrService,
          useValue: { error: () => {}, success: () => {}, warning: () => {} },
        },
      ],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(
      ProjectTeamAssessmentEvaluationFormComponent,
    );
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
