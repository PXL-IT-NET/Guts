import { ChangeDetectorRef, Component, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ProjectTeamAssessmentService } from "src/app/services";
import { PeerAssessmentModel } from "src/app/viewmodels/projectassessment.model";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  selector: "app-project-team-assessment-evaluation-form",
  templateUrl: "./project-team-assessment-evaluation-form.component.html",
})
export class ProjectTeamAssessmentEvaluationFormComponent
  implements OnInit, OnDestroy
{
  private destroy$ = new Subject<void>();
  private projectAssessmentId: number;
  private teamId: number;

  public peerAssessments: PeerAssessmentModel[];

  constructor(
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private route: ActivatedRoute,
    private router: Router,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef,
  ) {
    this.peerAssessments = [];
    this.projectAssessmentId = 0;
    this.teamId = 0;
  }

  ngOnInit(): void {
    this.projectAssessmentId = this.route.snapshot.params["assessmentId"];
    this.teamId = this.route.snapshot.params["teamId"];
    this.projectTeamAssessmentService
      .getPeerAssessmentsOfUser(this.projectAssessmentId, this.teamId)
      .pipe(takeUntil(this.destroy$))
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

        this.cdr.detectChanges();
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
        .pipe(takeUntil(this.destroy$))
        .subscribe((result) => {
          if (result.success) {
            if (validPeerAssessments.length == this.peerAssessments.length) {
              this.toastr.success("Peer assessment saved");
            } else {
              this.toastr.warning(
                "Peer assessment only partialy saved. Not all scores were filled in.",
              );
            }

            this.router.navigate(["../../../../"], { relativeTo: this.route });
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Cannot save assessments",
            );
          }

          this.cdr.detectChanges();
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

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
