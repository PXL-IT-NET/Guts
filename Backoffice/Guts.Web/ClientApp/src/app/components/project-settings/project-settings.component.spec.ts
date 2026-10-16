import { NO_ERRORS_SCHEMA } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { ToastrService } from "ngx-toastr";
import { of } from "rxjs";
import { ProjectService } from "src/app/services";
import {
  createActivatedRouteProvider,
  createToastrSpy,
} from "src/app/testing/test-helpers";
import { GetResult, PostResult } from "src/app/util/result";
import { ProjectSettingsComponent } from "./project-settings.component";

describe("ProjectSettingsComponent", () => {
  let component: ProjectSettingsComponent;
  let fixture: ComponentFixture<ProjectSettingsComponent>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(() => {
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "getProjectDetails",
      "updateProject",
    ]);
    toastrSpy = createToastrSpy();

    TestBed.configureTestingModule({
      declarations: [ProjectSettingsComponent],
      imports: [FormsModule, ReactiveFormsModule],
      schemas: [NO_ERRORS_SCHEMA],
      providers: [
        createActivatedRouteProvider({ code: "P1" }, { courseId: "2" }),
        { provide: ProjectService, useValue: projectServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    });
    fixture = TestBed.createComponent(ProjectSettingsComponent);
    component = fixture.componentInstance;
  });

  it("ngOnInit should load project details into form and handle update submission", () => {
    projectServiceSpy.getProjectDetails.and.returnValue(
      of(
        GetResult.success({
          id: 1,
          code: "P1",
          description: "Project One",
          components: [],
          teams: [],
          assignments: [],
        }),
      ),
    );

    component.ngOnInit();

    expect(projectServiceSpy.getProjectDetails).toHaveBeenCalledWith(2, "P1");
    expect(component.editProjectForm.value).toEqual({
      code: "P1",
      description: "Project One",
    });

    projectServiceSpy.updateProject.and.returnValue(of(PostResult.success()));
    component.editProjectForm.controls.description.setValue("Updated");
    component.onSubmit();
    expect(projectServiceSpy.updateProject).toHaveBeenCalledWith(2, "P1", {
      description: "Updated",
    });
    expect(toastrSpy.success).toHaveBeenCalled();

    projectServiceSpy.updateProject.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Update err" }),
    );
    component.onSubmit();
    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Update err",
      "Could not update project",
    );
  });

  it("should handle load error, isInvalid, and onAssignmentChanged", () => {
    projectServiceSpy.getProjectDetails.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Load err", value: null }),
    );
    component.ngOnInit();
    expect(toastrSpy.error).toHaveBeenCalled();

    const ctrl = component.editProjectForm.controls.description;
    ctrl.markAsDirty();
    expect(component.isInvalid(ctrl)).toBeTrue();

    projectServiceSpy.getProjectDetails.calls.reset();
    component.onAssignmentChanged(null);
    expect(projectServiceSpy.getProjectDetails).toHaveBeenCalled();
  });
});
