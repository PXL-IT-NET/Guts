import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BsModalRef } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { ProjectAssessmentService } from "../../services";
import {
  createModalRefSpy,
  createToastrSpy,
} from "../../testing/test-helpers";
import { PostResult } from "../../util/result";
import { ProjectAssessmentModel } from "../../viewmodels/projectassessment.model";
import { ProjectAssessmentEditComponent } from "./project-assessment-edit.component";

describe("ProjectAssessmentEditComponent", () => {
  let component: ProjectAssessmentEditComponent;
  let fixture: ComponentFixture<ProjectAssessmentEditComponent>;
  let serviceSpy: jasmine.SpyObj<ProjectAssessmentService>;
  let modalRefSpy: jasmine.SpyObj<BsModalRef>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    serviceSpy = jasmine.createSpyObj<ProjectAssessmentService>(
      "ProjectAssessmentService",
      ["updateProjectAssessment"],
    );
    modalRefSpy = createModalRefSpy();
    toastrSpy = createToastrSpy();

    TestBed.overrideComponent(ProjectAssessmentEditComponent, {
      set: { template: "" },
    });
    TestBed.configureTestingModule({
      declarations: [ProjectAssessmentEditComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: BsModalRef, useValue: modalRefSpy },
        { provide: ProjectAssessmentService, useValue: serviceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ProjectAssessmentEditComponent);
    component = fixture.componentInstance;
    component.projectId = 1;
    component.projectAssessment = new ProjectAssessmentModel({
      id: 7,
      description: "Sprint 1",
      openOnUtc: new Date(),
      deadlineUtc: new Date(Date.now() + 86400000),
      teamStatus: null,
    });
    component.ngOnInit();
  });

  it("onSubmit should update assessment and emit assessmentEdited on success", () => {
    serviceSpy.updateProjectAssessment.and.returnValue(of(PostResult.success()));
    let emitted = false;
    component.assessmentEdited.subscribe(() => (emitted = true));

    component.onSubmit();

    expect(serviceSpy.updateProjectAssessment).toHaveBeenCalled();
    expect(emitted).toBeTrue();
    expect(modalRefSpy.hide).toHaveBeenCalled();
  });

  it("onSubmit should show error toastr on failure and validate controls", () => {
    serviceSpy.updateProjectAssessment.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Update err" }),
    );

    component.onSubmit();
    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Update err",
      "Could not update project assessment",
    );

    const ctrl = component.editAssessmentForm.controls.description;
    ctrl.setValue("");
    ctrl.markAsTouched();
    expect(component.isInvalid(ctrl)).toBeTrue();
  });
});

