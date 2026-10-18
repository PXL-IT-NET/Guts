import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import moment from "moment";
import { BsModalService } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { of, Subject } from "rxjs";
import {
  AuthService,
  PeriodProvider,
  ProjectService,
  ProjectTeamAssessmentService,
} from "src/app/services";
import { ProjectAssessmentService } from "src/app/services/project.assessment.service";
import {
  createActivatedRouteProvider,
  createModalRefSpy,
  createModalServiceSpy,
  createToastrSpy,
} from "src/app/testing/test-helpers";
import { GetResult, PostResult } from "src/app/util/result";
import {
  ProjectAssessmentModel,
  ProjectTeamAssessmentStatusModel,
} from "src/app/viewmodels/projectassessment.model";
import { UserProfile } from "src/app/viewmodels/user.model";
import { ProjectAssessmentOverviewComponent } from "./project-assessment-overview.component";

describe("ProjectAssessmentOverviewComponent", () => {
  let component: ProjectAssessmentOverviewComponent;
  let fixture: ComponentFixture<ProjectAssessmentOverviewComponent>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let assessmentServiceSpy: jasmine.SpyObj<ProjectAssessmentService>;
  let teamAssessmentServiceSpy: jasmine.SpyObj<ProjectTeamAssessmentService>;
  let modalServiceSpy: jasmine.SpyObj<BsModalService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;
  let periodProvider: PeriodProvider;

  beforeEach(async () => {
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "getProjectDetails",
    ]);
    assessmentServiceSpy = jasmine.createSpyObj<ProjectAssessmentService>(
      "ProjectAssessmentService",
      ["getAssessmentsOfProject", "deleteProjectAssessment"],
    );
    teamAssessmentServiceSpy =
      jasmine.createSpyObj<ProjectTeamAssessmentService>(
        "ProjectTeamAssessmentService",
        ["getStatusOfProjectTeamAssessment"],
      );
    modalServiceSpy = createModalServiceSpy(
      createModalRefSpy({
        assessmentAdded: new Subject(),
        assessmentEdited: new Subject(),
      }),
    );
    authServiceSpy = jasmine.createSpyObj<AuthService>("AuthService", [
      "getUserProfile",
    ]);
    authServiceSpy.getUserProfile.and.returnValue(of(new UserProfile()));
    toastrSpy = createToastrSpy();
    periodProvider = new PeriodProvider();
    periodProvider.period = {
      id: 1,
      description: "Active",
      from: null,
      until: null,
      isActive: true,
    };

    TestBed.overrideComponent(ProjectAssessmentOverviewComponent, {
      set: { template: "" },
    });
    await TestBed.configureTestingModule({
      declarations: [ProjectAssessmentOverviewComponent],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider({ code: "P1" }, { courseId: "2" }),
        { provide: ProjectService, useValue: projectServiceSpy },
        { provide: ProjectAssessmentService, useValue: assessmentServiceSpy },
        {
          provide: ProjectTeamAssessmentService,
          useValue: teamAssessmentServiceSpy,
        },
        { provide: BsModalService, useValue: modalServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
        { provide: PeriodProvider, useValue: periodProvider },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectAssessmentOverviewComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("ngOnInit should load project details, assessments, and team assessment status", () => {
    const assessment = new ProjectAssessmentModel({
      id: 5,
      description: "PA1",
      openOnUtc: moment.utc().subtract(1, "day"),
      deadlineUtc: moment.utc().add(1, "day"),
      teamStatus: null,
    });
    const status = new ProjectTeamAssessmentStatusModel({
      id: 10,
      teamId: 3,
      isComplete: true,
      peersThatNeedToEvaluateOthers: [],
    });

    projectServiceSpy.getProjectDetails.and.returnValue(
      of(
        GetResult.success({
          id: 9,
          code: "P1",
          description: "Project 1",
          components: [],
          teams: [{ id: 3, name: "Team 3" }],
          assignments: [],
        }),
      ),
    );
    assessmentServiceSpy.getAssessmentsOfProject.and.returnValue(
      of(GetResult.success([assessment])),
    );
    teamAssessmentServiceSpy.getStatusOfProjectTeamAssessment.and.returnValue(
      of(GetResult.success(status)),
    );

    component.ngOnInit();

    expect(component.selectedTeamId).toBe(3);
    expect(component.assessments).toEqual([assessment]);
    expect(assessment.teamStatus).toBe(status);
  });

  it("openAssessmentAddModal, openAssessmentEditModal, and deleteAssessment should trigger modals and service calls", () => {
    projectServiceSpy.getProjectDetails.and.returnValue(
      of(
        GetResult.success({
          id: 9,
          code: "P1",
          description: "Project 1",
          components: [],
          teams: [],
          assignments: [],
        }),
      ),
    );
    assessmentServiceSpy.getAssessmentsOfProject.and.returnValue(
      of(GetResult.success([])),
    );
    component.ngOnInit();

    component.openAssessmentAddModal();
    expect(modalServiceSpy.show).toHaveBeenCalled();

    const assessment = new ProjectAssessmentModel();
    assessment.id = 8;
    assessment.description = "PA8";
    component.openAssessmentEditModal(assessment);
    expect(modalServiceSpy.show).toHaveBeenCalledTimes(2);

    spyOn(window, "confirm").and.returnValue(true);
    assessmentServiceSpy.deleteProjectAssessment.and.returnValue(
      of(PostResult.success()),
    );
    component.deleteAssessment(assessment);
    expect(assessmentServiceSpy.deleteProjectAssessment).toHaveBeenCalledWith(8);
    expect(toastrSpy.success).toHaveBeenCalled();
  });
});
