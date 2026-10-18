import { TestBed } from "@angular/core/testing";
import { ProjectTeamAddComponent } from "./project-team-add.component";
import { ProjectService } from "../../services";
import { CreateResult, PostResult } from "../../util/result";
import { TeamGenerationModel } from "../../viewmodels/team.model";
import { of } from "rxjs";
import {
  createModalRefSpy,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("ProjectTeamAddComponent", () => {
  let component: ProjectTeamAddComponent;
  let modalRefSpy: ReturnType<typeof createModalRefSpy>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let toastrSpy: ReturnType<typeof createToastrSpy>;

  beforeEach(() => {
    modalRefSpy = createModalRefSpy();
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "generateTeams",
      "addTeam",
    ]);
    toastrSpy = createToastrSpy();

    component = TestBed.runInInjectionContext(() => new ProjectTeamAddComponent(
      modalRefSpy as any,
      projectServiceSpy,
      toastrSpy,
    ));
    component.courseId = 1;
    component.projectCode = "PROJ1";
    component.ngOnInit();
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("should initialize form on ngOnInit", () => {
    expect(component.addTeamForm).toBeDefined();
    expect(component.addTeamForm.controls.name.value).toBe("");
    expect(component.addTeamForm.controls.createMultipleTeams.value).toBeFalse();
    expect(component.addTeamForm.controls.teamNumberFrom.value).toBe(1);
    expect(component.addTeamForm.controls.teamNumberTo.value).toBe(1);
  });

  it("should not submit when form is invalid", () => {
    component.addTeamForm.controls.name.setValue("");
    component.onSubmit();
    expect(projectServiceSpy.addTeam).not.toHaveBeenCalled();
    expect(projectServiceSpy.generateTeams).not.toHaveBeenCalled();
  });

  describe("single team creation", () => {
    it("should add single team, hide modal, and emit teamsAdded on success", () => {
      projectServiceSpy.addTeam.and.returnValue(
        of(CreateResult.success({ id: 10, name: "Team Alpha", members: [] })),
      );
      spyOn(component.teamsAdded, "emit");

      component.addTeamForm.patchValue({
        name: "Team Alpha",
        createMultipleTeams: false,
      });
      component.onSubmit();

      expect(projectServiceSpy.addTeam).toHaveBeenCalledWith(
        1,
        "PROJ1",
        "Team Alpha",
      );
      expect(modalRefSpy.hide).toHaveBeenCalled();
      expect(component.teamsAdded.emit).toHaveBeenCalled();
      expect(component.loading).toBeFalse();
    });

    it("should show toastr error when adding single team fails", () => {
      const failResult = new CreateResult<any>();
      failResult.success = false;
      failResult.message = "Duplicate team";
      projectServiceSpy.addTeam.and.returnValue(of(failResult));

      component.addTeamForm.patchValue({
        name: "Team Alpha",
        createMultipleTeams: false,
      });
      component.onSubmit();

      expect(toastrSpy.error).toHaveBeenCalledWith(
        "Duplicate team",
        "Could not generate teams",
      );
      expect(modalRefSpy.hide).not.toHaveBeenCalled();
    });
  });

  describe("multiple teams generation", () => {
    it("should generate multiple teams, hide modal, and emit teamsAdded on success", () => {
      projectServiceSpy.generateTeams.and.returnValue(of(PostResult.success()));
      spyOn(component.teamsAdded, "emit");

      component.addTeamForm.patchValue({
        name: "Team",
        createMultipleTeams: true,
        teamNumberFrom: 1,
        teamNumberTo: 5,
      });
      component.onSubmit();

      expect(projectServiceSpy.generateTeams).toHaveBeenCalledWith(
        1,
        "PROJ1",
        new TeamGenerationModel("Team", 1, 5),
      );
      expect(modalRefSpy.hide).toHaveBeenCalled();
      expect(component.teamsAdded.emit).toHaveBeenCalled();
    });

    it("should show toastr error with fallback message when generating multiple teams fails", () => {
      const failResult = new PostResult();
      failResult.success = false;
      failResult.message = "";
      projectServiceSpy.generateTeams.and.returnValue(of(failResult));

      component.addTeamForm.patchValue({
        name: "Team",
        createMultipleTeams: true,
        teamNumberFrom: 1,
        teamNumberTo: 5,
      });
      component.onSubmit();

      expect(toastrSpy.error).toHaveBeenCalledWith(
        "unknown error",
        "Could not generate teams",
      );
    });
  });

  it("should validate control via isInvalid", () => {
    const nameCtrl = component.addTeamForm.controls.name;
    expect(component.isInvalid(nameCtrl)).toBeFalse();
    nameCtrl.markAsDirty();
    expect(component.isInvalid(nameCtrl)).toBeTrue();
    nameCtrl.setValue("Valid");
    expect(component.isInvalid(nameCtrl)).toBeFalse();
  });
});

