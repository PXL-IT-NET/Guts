import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService, ProjectTeamAssessmentService } from 'src/app/services';
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

  public savePeerAssessments(){
    this.projectTeamAssessmentService.savePeerAssessment(this.projectAssessmentId, this.teamId, this.peerAssessments).subscribe(result => {
      if (result.success) {
        this.toastr.success("Peer assessment saved");
        this.router.navigate(['../../../../'], { relativeTo: this.route });
      } else{
        this.toastr.error((result.message || "unknown error"), "Cannot save assessments");
      }
    });
  }

}
