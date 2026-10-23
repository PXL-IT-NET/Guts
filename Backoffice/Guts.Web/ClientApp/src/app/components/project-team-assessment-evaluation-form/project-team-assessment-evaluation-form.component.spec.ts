import { TestBed } from "@angular/core/testing";
import { ProjectTeamAssessmentEvaluationFormComponent } from "./project-team-assessment-evaluation-form.component";
import { ProjectTeamAssessmentService } from "src/app/services";
import { GetResult, PostResult } from "src/app/util/result";
import { PeerAssessmentModel } from "src/app/viewmodels/projectassessment.model";
import { of } from "rxjs";
import {
  ActivatedRouteStub,
  createRouterSpy,
  createToastrSpy,
} from "../../testing/test-helpers";

describe("ProjectTeamAssessmentEvaluationFormComponent", () => {
  let component: ProjectTeamAssessmentEvaluationFormComponent;
  let serviceSpy: jasmine.SpyObj<ProjectTeamAssessmentService>;
  let routeStub: ActivatedRouteStub;
  let routerSpy: ReturnType<typeof createRouterSpy>;
  let toastrSpy: ReturnType<typeof createToastrSpy>;

  beforeEach(() => {
    serviceSpy = jasmine.createSpyObj<ProjectTeamAssessmentService>(
      "ProjectTeamAssessmentService",
      ["getPeerAssessmentsOfUser", "savePeerAssessment"],
    );
    routeStub = new ActivatedRouteStub({ assessmentId: 5, teamId: 10 });
    routerSpy = createRouterSpy();
    toastrSpy = createToastrSpy();

    component = TestBed.runInInjectionContext(() => new ProjectTeamAssessmentEvaluationFormComponent(
      serviceSpy,
      routeStub as any,
      routerSpy,
      toastrSpy,
    ));
  });

  function createPeerAssessment(
    contribution: number,
    cooperation: number,
    effort: number,
    explanation: string = "",
  ): PeerAssessmentModel {
    const pa = new PeerAssessmentModel();
    pa.contributionScore = contribution;
    pa.cooperationScore = cooperation;
    pa.effortScore = effort;
    pa.explanation = explanation;
    return pa;
  }

  it("should load peer assessments on ngOnInit", () => {
    const assessments = [createPeerAssessment(3, 3, 3)];
    serviceSpy.getPeerAssessmentsOfUser.and.returnValue(
      of(GetResult.success(assessments)),
    );

    component.ngOnInit();

    expect(serviceSpy.getPeerAssessmentsOfUser).toHaveBeenCalledWith(5, 10);
    expect(component.peerAssessments).toEqual(assessments);
  });

  it("should show toastr error when loading peer assessments fails", () => {
    const failResult = new GetResult<PeerAssessmentModel[]>();
    failResult.success = false;
    failResult.message = "API error";
    serviceSpy.getPeerAssessmentsOfUser.and.returnValue(of(failResult));

    component.ngOnInit();

    expect(toastrSpy.error).toHaveBeenCalledWith(
      "Could not load assessments from API. Message: API error",
      "System error",
    );
  });

  describe("savePeerAssessments", () => {
    beforeEach(() => {
      serviceSpy.getPeerAssessmentsOfUser.and.returnValue(
        of(GetResult.success([])),
      );
      component.ngOnInit();
    });

    it("should show error when no valid peer assessments exist", () => {
      component.peerAssessments = [createPeerAssessment(-1, 3, 3)];
      component.savePeerAssessments();

      expect(toastrSpy.error).toHaveBeenCalledWith(
        "Fill in the scores for at least one team member",
        "Cannot save assessments",
      );
      expect(serviceSpy.savePeerAssessment).not.toHaveBeenCalled();
    });

    it("should show success and navigate when all peer assessments are valid and saved", () => {
      component.peerAssessments = [
        createPeerAssessment(3, 3, 3),
        createPeerAssessment(4, 4, 4, "Good work"),
      ];
      serviceSpy.savePeerAssessment.and.returnValue(of(PostResult.success()));

      component.savePeerAssessments();

      expect(serviceSpy.savePeerAssessment).toHaveBeenCalledWith(
        5,
        10,
        component.peerAssessments,
      );
      expect(toastrSpy.success).toHaveBeenCalledWith("Peer assessment saved");
      expect(routerSpy.navigate).toHaveBeenCalledWith(["../../../../"], {
        relativeTo: routeStub as any,
      });
    });

    it("should show warning and navigate when only partial assessments are valid", () => {
      const valid = createPeerAssessment(3, 3, 3);
      const invalid = createPeerAssessment(-1, -1, -1);
      component.peerAssessments = [valid, invalid];
      serviceSpy.savePeerAssessment.and.returnValue(of(PostResult.success()));

      component.savePeerAssessments();

      expect(serviceSpy.savePeerAssessment).toHaveBeenCalledWith(5, 10, [
        valid,
      ]);
      expect(toastrSpy.warning).toHaveBeenCalledWith(
        "Peer assessment only partialy saved. Not all scores were filled in.",
      );
      expect(routerSpy.navigate).toHaveBeenCalled();
    });

    it("should show error when savePeerAssessment fails", () => {
      component.peerAssessments = [createPeerAssessment(3, 3, 3)];
      const failResult = new PostResult();
      failResult.success = false;
      failResult.message = "Save failed";
      serviceSpy.savePeerAssessment.and.returnValue(of(failResult));

      component.savePeerAssessments();

      expect(toastrSpy.error).toHaveBeenCalledWith(
        "Save failed",
        "Cannot save assessments",
      );
      expect(routerSpy.navigate).not.toHaveBeenCalled();
    });
  });

  describe("isInvalid", () => {
    it("should return false when all scores are average (3) even without explanation", () => {
      expect(component.isInvalid(createPeerAssessment(3, 3, 3, ""))).toBeFalse();
    });

    it("should return true when any score is not 3 and explanation is empty or whitespace", () => {
      expect(component.isInvalid(createPeerAssessment(4, 3, 3, ""))).toBeTrue();
      expect(
        component.isInvalid(createPeerAssessment(3, 2, 3, "   ")),
      ).toBeTrue();
    });

    it("should return false when score is not 3 and explanation is provided", () => {
      expect(
        component.isInvalid(createPeerAssessment(3, 3, 5, "Did extra work")),
      ).toBeFalse();
    });
  });
});

