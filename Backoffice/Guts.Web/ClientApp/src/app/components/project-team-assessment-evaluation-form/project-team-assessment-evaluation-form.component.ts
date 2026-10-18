import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ProjectTeamAssessmentService } from "src/app/services";
import { PeerAssessmentModel } from "src/app/viewmodels/projectassessment.model";

@Component({
  standalone: false,
  selector: "app-project-team-assessment-evaluation-form",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-team-assessment-evaluation-form.component.html",
})
export class ProjectTeamAssessmentEvaluationFormComponent
  implements OnInit, OnDestroy
{
  private destroyRef = inject(DestroyRef);
  private projectAssessmentId: number;
  private teamId: number;

  private _peerAssessments = signal<PeerAssessmentModel[]>([]);
  public get peerAssessments(): PeerAssessmentModel[] {
    return this._peerAssessments();
  }
  public set peerAssessments(v: PeerAssessmentModel[]) {
    this._peerAssessments.set(v);
  }

  constructor(
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private route: ActivatedRoute,
    private router: Router,
    private toastr: ToastrService,
  ) {
    this.projectAssessmentId = 0;
    this.teamId = 0;
  }

  ngOnInit(): void {
    this.projectAssessmentId = this.route.snapshot.params["assessmentId"];
    this.teamId = this.route.snapshot.params["teamId"];
    this.projectTeamAssessmentService
      .getPeerAssessmentsOfUser(this.projectAssessmentId, this.teamId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.success) {
          this.peerAssessments = result.value;
        } else {
          this.toastr.error(
            "Could not load assessments from API. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  public savePeerAssessments() {
    let validPeerAssessments = this.peerAssessments.filter(
      (pa) =>
        pa.contributionScore >= 0 &&
        pa.cooperationScore >= 0 &&
        pa.effortScore >= 0,
    );
    if (validPeerAssessments.length == 0) {
      this.toastr.error(
        "Fill in the scores for at least one team member",
        "Cannot save assessments",
      );
    } else {
      this.projectTeamAssessmentService
        .savePeerAssessment(
          this.projectAssessmentId,
          this.teamId,
          validPeerAssessments,
        )
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          if (result.success) {
            if (validPeerAssessments.length == this.peerAssessments.length) {
              this.toastr.success("Peer assessment saved");
            } else {
              this.toastr.warning(
                "Peer assessment only partialy saved. Not all scores were filled in.",
              );
            }

            void this.router.navigate(["../../../../"], { relativeTo: this.route });
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Cannot save assessments",
            );
          }
        });
    }
  }

  public isInvalid(peerAssessment: PeerAssessmentModel): boolean {
    const isNotAverage =
      peerAssessment.contributionScore != 3 ||
      peerAssessment.cooperationScore != 3 ||
      peerAssessment.effortScore != 3;
    const explanationIsMisssing =
      isNotAverage &&
      (!peerAssessment.explanation ||
        peerAssessment.explanation.trim().length == 0);
    return explanationIsMisssing;
  }

  ngOnDestroy(): void {}
}
