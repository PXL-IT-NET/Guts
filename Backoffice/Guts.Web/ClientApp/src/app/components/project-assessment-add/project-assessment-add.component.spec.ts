import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BsModalRef } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { ProjectAssessmentService } from "../../services/project.assessment.service";
import {
  createModalRefSpy,
  createToastrSpy,
} from "../../testing/test-helpers";
import { CreateResult } from "../../util/result";
import { ProjectAssessmentModel } from "../../viewmodels/projectassessment.model";
import { ProjectAssessmentAddComponent } from "./project-assessment-add.component";

describe("ProjectAssessmentAddComponent", () => {
  let component: ProjectAssessmentAddComponent;
  let fixture: ComponentFixture<ProjectAssessmentAddComponent>;
  let serviceSpy: jasmine.SpyObj<ProjectAssessmentService>;
  let modalRefSpy: jasmine.SpyObj<BsModalRef>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    serviceSpy = jasmine.createSpyObj<ProjectAssessmentService>(
      "ProjectAssessmentService",
      ["addProjectAssessment"],
    );
    modalRefSpy = createModalRefSpy();
    toastrSpy = createToastrSpy();

    TestBed.overrideComponent(ProjectAssessmentAddComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [ProjectAssessmentAddComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: BsModalRef, useValue: modalRefSpy },
        { provide: ProjectAssessmentService, useValue: serviceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ProjectAssessmentAddComponent);
    component = fixture.componentInstance;
    component.projectId = 4;
    component.ngOnInit();
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("onSubmit should validate form and emit assessmentAdded on success", () => {
    component.onSubmit();
    expect(serviceSpy.addProjectAssessment).not.toHaveBeenCalled();
    expect(
      component.isInvalid(component.addAssessmentForm.controls.description),
    ).toBeTrue();

    const created = new ProjectAssessmentModel();
    serviceSpy.addProjectAssessment.and.returnValue(
      of(CreateResult.success(created)),
    );
    let emitted: any;
    component.assessmentAdded.subscribe((a) => (emitted = a));

    component.addAssessmentForm.setValue({
      description: "Sprint 1",
      openOn: new Date(),
      deadline: new Date(Date.now() + 86400000),
    });
    component.onSubmit();

    expect(serviceSpy.addProjectAssessment).toHaveBeenCalled();
    expect(emitted).toBe(created);
    expect(modalRefSpy.hide).toHaveBeenCalled();
  });

  it("onSubmit should show error toastr when addProjectAssessment fails", () => {
    serviceSpy.addProjectAssessment.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Err", value: null }),
    );
    component.addAssessmentForm.setValue({
      description: "Sprint 1",
      openOn: new Date(),
      deadline: new Date(Date.now() + 86400000),
    });

    component.onSubmit();

    expect(toastrSpy.error).toHaveBeenCalled();
  });
});

