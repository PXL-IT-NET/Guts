import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChartData, ChartOptions } from 'chart.js';
import { ToastrService } from 'ngx-toastr';
import { ProjectService, ProjectTeamAssessmentService } from 'src/app/services';
import { IAssessmentResultModel } from 'src/app/viewmodels/projectassessment.model';

@Component({
  selector: 'app-project-team-assessment-my-result',
  templateUrl: './project-team-assessment-my-result.component.html'
})
export class ProjectTeamAssessmentMyResultComponent implements OnInit {

  public result: IAssessmentResultModel;

  public chartData: ChartData<'radar', number[]>;

  public radarChartOptions: ChartOptions<'radar'> = {
    scales: {
      r: {
        beginAtZero: true,
        suggestedMin: 0,
        suggestedMax: 5,
        ticks: {
          stepSize: 1
        }
      }
    }
  };

  constructor(
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private projectService: ProjectService,
    private toastr: ToastrService,
    private route: ActivatedRoute) {
    this.result = null;

    this.chartData = {
      datasets: [],
      labels: [],
    };
  }

  ngOnInit(): void {

    let courseId = +this.route.parent.snapshot.params['courseId']
    let projectCode = this.route.snapshot.params['code'];
    let assessmentId = this.route.snapshot.params['assessmentId'];

    this.route.params.subscribe(params => {
      let teamId = params['teamId'];

      this.projectTeamAssessmentService.getMyResultOfProjectTeamAssessment(assessmentId, teamId).subscribe(result => {
        if (result.success) {
          this.result = result.value;

          this.chartData.datasets.push({
            label: 'Peers',
            data: [this.result.averageResult.peerValue, this.result.effortResult.peerValue, this.result.contributionResult.peerValue, this.result.cooperationResult.peerValue]
          });
          this.chartData.datasets.push({
            label: 'Self',
            data: [this.result.averageResult.selfValue, this.result.effortResult.selfValue, this.result.contributionResult.selfValue, this.result.cooperationResult.selfValue]
          });
          this.chartData.labels = ['Average', 'Effort', 'Contribution', 'Cooperation' ];

        } else {
          this.toastr.warning("Could not retrieve project team assment result. Message: " + (result.message || "unknown error"), "API error");
        }
      });
    });
  }

}
