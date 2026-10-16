import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BsModalRef } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { ProjectService } from "src/app/services";
import {
  createModalRefSpy,
  createToastrSpy,
} from "src/app/testing/test-helpers";
import { CreateResult } from "src/app/util/result";
import { IProjectDetailsModel } from "src/app/viewmodels/project.model";
import { ProjectAddComponent } from "./project-add.component";

describe("ProjectAddComponent", () => {
  let component: ProjectAddComponent;
  let fixture: ComponentFixture<ProjectAddComponent>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let modalRefSpy: jasmine.SpyObj<BsModalRef>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "addProject",
    ]);
    modalRefSpy = createModalRefSpy();
    toastrSpy = createToastrSpy();

    TestBed.configureTestingModule({
      declarations: [ProjectAddComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        { provide: BsModalRef, useValue: modalRefSpy },
        { provide: ProjectService, useValue: projectServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ProjectAddComponent);
    component = fixture.componentInstance;
    component.courseId = 3;
    component.ngOnInit();
  });

  it("onSubmit should validate form and call addProject on valid submission", () => {
    component.onSubmit();
    expect(projectServiceSpy.addProject).not.toHaveBeenCalled();

    const codeCtrl = component.addProjectForm.controls.code;
    codeCtrl.markAsTouched();
    expect(component.isInvalid(codeCtrl)).toBeTrue();

    const added: IProjectDetailsModel = {
      id: 1,
      code: "P1",
      description: "Project 1",
      components: [],
      teams: [],
      assignments: [],
    };
    projectServiceSpy.addProject.and.returnValue(
      of(CreateResult.success(added)),
    );

    let emitted: IProjectDetailsModel;
    component.projectAdded.subscribe((p) => (emitted = p));

    component.addProjectForm.setValue({ code: "P1", description: "Project 1" });
    component.onSubmit();

    expect(projectServiceSpy.addProject).toHaveBeenCalledWith(3, {
      code: "P1",
      description: "Project 1",
    });
    expect(modalRefSpy.hide).toHaveBeenCalled();
    expect(emitted).toBe(added);
  });

  it("onSubmit should show error toastr when addProject fails", () => {
    projectServiceSpy.addProject.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Duplicate", value: null }),
    );
    component.addProjectForm.setValue({ code: "P1", description: "Project 1" });

    component.onSubmit();

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Duplicate",
      "Could not add project",
    );
  });
});
