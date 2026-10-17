import { ProjectTeamEditComponent } from "./project-team-edit.component";
import { ProjectService } from "../../services";
import { PostResult } from "../../util/result";
import { of } from "rxjs";
import {
  createModalRefSpy,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("ProjectTeamEditComponent", () => {
  let component: ProjectTeamEditComponent;
  let modalRefSpy: ReturnType<typeof createModalRefSpy>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let toastrSpy: ReturnType<typeof createToastrSpy>;

  beforeEach(() => {
    modalRefSpy = createModalRefSpy();
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "updateTeam",
      "removeFromTeam",
    ]);
    toastrSpy = createToastrSpy();

    component = new ProjectTeamEditComponent(
      modalRefSpy as any,
      projectServiceSpy,
      toastrSpy,
    );
    component.courseId = 1;
    component.projectCode = "PROJ1";
    component.team = {
      id: 10,
      name: "Initial Team",
      members: [
        { userId: 100, name: "Alice" },
        { userId: 101, name: "Bob" },
      ],
    };
    component.ngOnInit();
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it("should patch team values onto form on ngOnInit", () => {
    expect(component.editTeamForm.controls.name.value).toBe("Initial Team");
  });

  it("should not submit when form is invalid", () => {
    component.editTeamForm.controls.name.setValue("");
    component.onSubmit();
    expect(projectServiceSpy.updateTeam).not.toHaveBeenCalled();
  });

  it("should update team and hide modal on submit success", () => {
    projectServiceSpy.updateTeam.and.returnValue(of(PostResult.success()));

    component.editTeamForm.controls.name.setValue("Renamed Team");
    component.onSubmit();

    expect(component.team.name).toBe("Renamed Team");
    expect(projectServiceSpy.updateTeam).toHaveBeenCalledWith(
      1,
      "PROJ1",
      component.team,
    );
    expect(modalRefSpy.hide).toHaveBeenCalled();
    expect(component.loading).toBeFalse();
  });

  it("should show toastr error when updateTeam fails", () => {
    const failResult = new PostResult();
    failResult.success = false;
    failResult.message = "Update failed";
    projectServiceSpy.updateTeam.and.returnValue(of(failResult));

    component.onSubmit();

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Update failed",
      "Could not update team",
    );
    expect(modalRefSpy.hide).not.toHaveBeenCalled();
  });

  describe("removeFromTeam", () => {
    it("should remove member when confirmed and service succeeds", () => {
      spyOn(window, "confirm").and.returnValue(true);
      projectServiceSpy.removeFromTeam.and.returnValue(of(PostResult.success()));

      component.removeFromTeam({ userId: 100, name: "Alice" });

      expect(projectServiceSpy.removeFromTeam).toHaveBeenCalledWith(
        1,
        "PROJ1",
        10,
        100,
      );
      expect(component.team.members.length).toBe(1);
      expect(component.team.members[0].userId).toBe(101);
    });

    it("should not call removeFromTeam when confirmation is cancelled", () => {
      spyOn(window, "confirm").and.returnValue(false);

      component.removeFromTeam({ userId: 100, name: "Alice" });

      expect(projectServiceSpy.removeFromTeam).not.toHaveBeenCalled();
      expect(component.team.members.length).toBe(2);
    });

    it("should show toastr error when removeFromTeam fails", () => {
      spyOn(window, "confirm").and.returnValue(true);
      const failResult = new PostResult();
      failResult.success = false;
      failResult.message = "";
      projectServiceSpy.removeFromTeam.and.returnValue(of(failResult));

      component.removeFromTeam({ userId: 100, name: "Alice" });

      expect(toastrSpy.error).toHaveBeenCalledWith(
        "unknown error",
        "Could not remove user from team",
      );
      expect(component.team.members.length).toBe(2);
    });
  });

  it("should validate control via isInvalid", () => {
    const nameCtrl = component.editTeamForm.controls.name;
    expect(component.isInvalid(nameCtrl)).toBeFalse();
    nameCtrl.setValue("");
    nameCtrl.markAsTouched();
    expect(component.isInvalid(nameCtrl)).toBeTrue();
  });
});

