import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";

import { ProjectTeamAssessmentDetailedResultsComponent } from "./project-team-assessment-detailed-results.component";
import { ProjectService, ProjectTeamAssessmentService } from "src/app/services";

describe("ProjectTeamAssessmentDetailedResultsComponent", () => {
  let component: ProjectTeamAssessmentDetailedResultsComponent;
  let fixture: ComponentFixture<ProjectTeamAssessmentDetailedResultsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ProjectTeamAssessmentDetailedResultsComponent],
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
          provide: ProjectTeamAssessmentService,
          useValue: {
            getDetailedResultsOfProjectTeamAssessment: () =>
              of({ success: false, message: "" }),
          },
        },
        {
          provide: ProjectService,
          useValue: {
            getProjectDetails: () => of({ success: false, message: "" }),
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
      ProjectTeamAssessmentDetailedResultsComponent,
    );
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
