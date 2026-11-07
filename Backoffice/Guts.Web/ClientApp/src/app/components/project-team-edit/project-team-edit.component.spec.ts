import { TestBed } from "@angular/core/testing";
import { ProjectTeamEditComponent } from "./project-team-edit.component";
import { ProjectService, ProjectTeamAssessmentService } from "../../services";
import { ProjectAssessmentService } from "../../services/project.assessment.service";
import { GetResult, PostResult } from "../../util/result";
import {
  ProjectAssessmentModel,
  ProjectTeamAssessmentStatusModel,
} from "../../viewmodels/projectassessment.model";
import { of } from "rxjs";
import {
  createModalRefSpy,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("ProjectTeamEditComponent", () => {
  let component: ProjectTeamEditComponent;
  let modalRefSpy: ReturnType<typeof createModalRefSpy>;
  let projectServiceSpy: jasmine.SpyObj<ProjectService>;
  let projectAssessmentServiceSpy: jasmine.SpyObj<ProjectAssessmentService>;
  let projectTeamAssessmentServiceSpy: jasmine.SpyObj<ProjectTeamAssessmentService>;
  let toastrSpy: ReturnType<typeof createToastrSpy>;

  beforeEach(() => {
    modalRefSpy = createModalRefSpy();
    projectServiceSpy = jasmine.createSpyObj<ProjectService>("ProjectService", [
      "updateTeam",
      "removeFromTeam",
      "getProjectDetails",
    ]);
    projectAssessmentServiceSpy =
      jasmine.createSpyObj<ProjectAssessmentService>(
        "ProjectAssessmentService",
        ["getAssessmentsOfProject"],
      );
    projectTeamAssessmentServiceSpy =
      jasmine.createSpyObj<ProjectTeamAssessmentService>(
        "ProjectTeamAssessmentService",
        ["getStatusOfProjectTeamAssessment", "setPeerInclusion"],
      );
    toastrSpy = createToastrSpy();

    projectServiceSpy.getProjectDetails.and.returnValue(
      of(
        GetResult.success({
          id: 5,
          code: "PROJ1",
          description: "Project 1",
          components: [],
          teams: [],
          assignments: [],
        }),
      ),
    );
    projectAssessmentServiceSpy.getAssessmentsOfProject.and.returnValue(
      of(GetResult.success([])),
    );

    component = TestBed.runInInjectionContext(
      () =>
        new ProjectTeamEditComponent(
          modalRefSpy as any,
          projectServiceSpy,
          toastrSpy,
          projectAssessmentServiceSpy,
          projectTeamAssessmentServiceSpy,
        ),
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
      expect(component.team.members).toHaveSize(1);
      expect(component.team.members[0].userId).toBe(101);
    });

    it("should not call removeFromTeam when confirmation is cancelled", () => {
      spyOn(window, "confirm").and.returnValue(false);

      component.removeFromTeam({ userId: 100, name: "Alice" });

      expect(projectServiceSpy.removeFromTeam).not.toHaveBeenCalled();
      expect(component.team.members).toHaveSize(2);
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
      expect(component.team.members).toHaveSize(2);
    });
  });

  it("should validate control via isInvalid", () => {
    const nameCtrl = component.editTeamForm.controls.name;
    expect(component.isInvalid(nameCtrl)).toBeFalse();
    nameCtrl.setValue("");
    nameCtrl.markAsTouched();
    expect(component.isInvalid(nameCtrl)).toBeTrue();
  });

  describe("peer inclusion in incomplete assessments", () => {
    it("should load incomplete open assessments, track hasPeerInclusionChanges on toggle, and persist on submit", () => {
      const openAssessment = new ProjectAssessmentModel({
        id: 50,
        description: "Sprint 1",
        openOnUtc: new Date(Date.now() - 86400000),
        deadlineUtc: new Date(Date.now() + 86400000),
        teamStatus: new ProjectTeamAssessmentStatusModel(),
      });
      projectAssessmentServiceSpy.getAssessmentsOfProject.and.returnValue(
        of(GetResult.success([openAssessment])),
      );
      projectTeamAssessmentServiceSpy.getStatusOfProjectTeamAssessment.and.returnValue(
        of(
          GetResult.success(
            new ProjectTeamAssessmentStatusModel({
              id: 1,
              teamId: 10,
              isComplete: false,
              peersThatNeedToEvaluateOthers: [],
              peerIds: [100],
            }),
          ),
        ),
      );

      component.ngOnInit();

      expect(component.incompleteAssessments).toHaveSize(1);
      expect(component.hasPeerInclusionChanges).toBeFalse();
      expect(
        component.isPeerIncluded(openAssessment, { userId: 100, name: "Alice" }),
      ).toBeTrue();
      expect(
        component.isPeerIncluded(openAssessment, { userId: 101, name: "Bob" }),
      ).toBeFalse();

      // Toggle Bob to included -> hasPeerInclusionChanges becomes true
      component.togglePeerInclusion(
        openAssessment,
        { userId: 101, name: "Bob" },
        true,
      );

      expect(
        component.isPeerIncluded(openAssessment, { userId: 101, name: "Bob" }),
      ).toBeTrue();
      expect(component.hasPeerInclusionChanges).toBeTrue();
      expect(
        projectTeamAssessmentServiceSpy.setPeerInclusion,
      ).not.toHaveBeenCalled();

      // Toggle Bob back to excluded -> hasPeerInclusionChanges becomes false
      component.togglePeerInclusion(
        openAssessment,
        { userId: 101, name: "Bob" },
        false,
      );
      expect(component.hasPeerInclusionChanges).toBeFalse();

      // Toggle Bob to included and cancel confirmation -> does not call setPeerInclusion
      component.togglePeerInclusion(
        openAssessment,
        { userId: 101, name: "Bob" },
        true,
      );
      expect(component.hasPeerInclusionChanges).toBeTrue();

      const confirmSpy = spyOn(window, "confirm").and.returnValue(false);
      component.onSubmit();

      expect(confirmSpy).toHaveBeenCalled();
      expect(confirmSpy.calls.mostRecent().args[0]).toContain(
        "- 'Sprint 1': include Bob",
      );
      expect(confirmSpy.calls.mostRecent().args[0]).toContain(
        "Including a peer means that team members who have already submitted the assessment form will need to edit and save their evaluation again",
      );
      expect(
        projectTeamAssessmentServiceSpy.setPeerInclusion,
      ).not.toHaveBeenCalled();
      expect(modalRefSpy.hide).not.toHaveBeenCalled();

      // Also exclude Alice and accept confirmation -> calls setPeerInclusion and hides modal
      component.togglePeerInclusion(
        openAssessment,
        { userId: 100, name: "Alice" },
        false,
      );
      confirmSpy.and.returnValue(true);
      projectTeamAssessmentServiceSpy.setPeerInclusion.and.returnValue(
        of(PostResult.success()),
      );

      component.onSubmit();

      expect(confirmSpy.calls.mostRecent().args[0]).toContain(
        "- 'Sprint 1': exclude Alice",
      );
      expect(confirmSpy.calls.mostRecent().args[0]).toContain(
        "Excluding a peer will permanently delete all peer evaluations already saved by or for that peer",
      );
      expect(
        projectTeamAssessmentServiceSpy.setPeerInclusion,
      ).toHaveBeenCalledWith(50, 10, 100, false);
      expect(
        projectTeamAssessmentServiceSpy.setPeerInclusion,
      ).toHaveBeenCalledWith(50, 10, 101, true);
      expect(projectServiceSpy.updateTeam).not.toHaveBeenCalled();
      expect(modalRefSpy.hide).toHaveBeenCalled();
    });
  });
});


