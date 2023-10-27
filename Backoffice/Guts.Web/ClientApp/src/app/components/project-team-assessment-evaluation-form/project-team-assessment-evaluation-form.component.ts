import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ProjectTeamAssessmentService } from 'src/app/services';
import { PeerAssessmentModel } from 'src/app/viewmodels/projectassessment.model';

@Component({
  selector: 'app-project-team-assessment-evaluation-form',
  templateUrl: './project-team-assessment-evaluation-form.component.html'
})
export class ProjectTeamAssessmentEvaluationFormComponent implements OnInit {

  private projectAssessmentId: number;
  private teamId: number;

  public peerAssessments: PeerAssessmentModel[];

  constructor(
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private route: ActivatedRoute,
    private router: Router,
    private toastr: ToastrService) {
    this.peerAssessments = [];
    this.projectAssessmentId = 0;
    this.teamId = 0;
  }

  ngOnInit(): void {
    this.projectAssessmentId = this.route.snapshot.params['assessmentId'];
    this.teamId = this.route.snapshot.params['teamId'];
    this.projectTeamAssessmentService.getPeerAssessmentsOfUser(this.projectAssessmentId, this.teamId).subscribe(result => {
      if (result.success) {
        this.peerAssessments = result.value;
      } else {
        this.toastr.error("Could not load assessments from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });

  }

  public savePeerAssessments() {
    let validPeerAssessments = this.peerAssessments.filter(pa => pa.contributionScore >= 0 && pa.cooperationScore >= 0 && pa.effortScore >= 0);
    if (validPeerAssessments.length == 0) {
      this.toastr.error("Fill in the scores for at least one team member", "Cannot save assessments");
    } else {
      this.projectTeamAssessmentService.savePeerAssessment(this.projectAssessmentId, this.teamId, validPeerAssessments).subscribe(result => {
        if (result.success) {
          if (validPeerAssessments.length == this.peerAssessments.length) {
            this.toastr.success("Peer assessment saved");
          } else {
            this.toastr.warning("Peer assessment only partialy saved. Not all scores were filled in.");
          }

          this.router.navigate(['../../../../'], { relativeTo: this.route });
        } else {
          this.toastr.error((result.message || "unknown error"), "Cannot save assessments");
        }
      });
    }
  }

}
