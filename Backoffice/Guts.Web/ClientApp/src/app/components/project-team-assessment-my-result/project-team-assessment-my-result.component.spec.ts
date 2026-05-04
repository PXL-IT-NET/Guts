import { ComponentFixture, TestBed } from "@angular/core/testing";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";

import { ProjectTeamAssessmentMyResultComponent } from "./project-team-assessment-my-result.component";
import { ProjectService, ProjectTeamAssessmentService } from "src/app/services";

describe("ProjectTeamAssessmentMyResultComponent", () => {
  let component: ProjectTeamAssessmentMyResultComponent;
  let fixture: ComponentFixture<ProjectTeamAssessmentMyResultComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ProjectTeamAssessmentMyResultComponent],
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
            getMyResultOfProjectTeamAssessment: () =>
              of({
                success: true,
                value: {
                  averageResult: {
                    selfScore: 3,
                    peerScore: 3,
                    selfValue: 3,
                    peerValue: 3,
                  },
                  effortResult: { selfValue: 3, peerValue: 3 },
                  contributionResult: { selfValue: 3, peerValue: 3 },
                  cooperationResult: { selfValue: 3, peerValue: 3 },
                },
              }),
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
    fixture = TestBed.createComponent(ProjectTeamAssessmentMyResultComponent);
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
