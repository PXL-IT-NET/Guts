import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ProjectTeamAssessmentService } from 'src/app/services';
import { IAssessmentResultModel } from 'src/app/viewmodels/projectassessment.model';

@Component({
  selector: 'app-project-team-assessment-detailed-results',
  templateUrl: './project-team-assessment-detailed-results.component.html'
})
export class ProjectTeamAssessmentDetailedResultsComponent implements OnInit {

  public results: IAssessmentResultModel[];

  constructor(
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private toastr: ToastrService,
    private route: ActivatedRoute) {
      this.results = [];
  }

  ngOnInit(): void {

    let assessmentId = this.route.snapshot.params['assessmentId'];
    let teamId = this.route.snapshot.params['teamId'];

    this.projectTeamAssessmentService.getDetailedResultsOfProjectTeamAssessment(assessmentId, teamId).subscribe(result => {
      if (result.success) {
        console.log('Received results');
        console.log(result.value);
        this.results = result.value;
      } else {
        this.toastr.warning("Could not retrieve project team assment status. Message: " + (result.message || "unknown error"), "API error");
      }
    });

  }

}
