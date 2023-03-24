import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChartData, ChartOptions } from 'chart.js';
import { ToastrService } from 'ngx-toastr';
import { ProjectTeamAssessmentService } from 'src/app/services';
import { IAssessmentResultModel } from 'src/app/viewmodels/projectassessment.model';

@Component({
  selector: 'app-project-team-assessment-detailed-results',
  templateUrl: './project-team-assessment-detailed-results.component.html'
})
export class ProjectTeamAssessmentDetailedResultsComponent implements OnInit {

  public results: IAssessmentResultModel[];
  public teamGrade: number;

  public selfChartData: ChartData<'bar', number[]>;
  public peerChartData: ChartData<'bar', number[]>;
  public peerAndSelfChartData: ChartData<'bar', number[]>;

  public chartOptions: ChartOptions<'bar'> = {
    scales: {
      y: {
        min: 0,
        max: 5,
        ticks: {
          callback: function (value, index, ticks) {
            if (value == 0) return 'No contribution (0)';
            if (value == 1) return 'Way below average (1)';
            if (value == 2) return 'Below average (2)';
            if (value == 3) return 'Average (3)';
            if (value == 4) return 'Above average (4)';
            if (value == 5) return 'Way above average (5)';
            return '';
          }
        }
      }
    }
  };

  constructor(
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private toastr: ToastrService,
    private route: ActivatedRoute) {
    this.results = [];
    this.teamGrade = 0;

    this.selfChartData = {
      datasets: [],
      labels: [],
    };

    this.peerChartData = {
      datasets: [],
      labels: [],
    };

    this.peerAndSelfChartData = {
      datasets: [],
      labels: [],
    };
  }

  ngOnInit(): void {

    let assessmentId = this.route.snapshot.params['assessmentId'];
    let teamId = this.route.snapshot.params['teamId'];

    this.projectTeamAssessmentService.getDetailedResultsOfProjectTeamAssessment(assessmentId, teamId).subscribe(result => {
      if (result.success) {
        this.results = result.value;
        this.calculateIndividualGrades();

        this.setChartData(this.peerAndSelfChartData,
          result => result.averageResult.average,
          result => result.effortResult.average,
          result => result.contributionResult.average,
          result => result.cooperationResult.average);

        this.setChartData(this.selfChartData,
          result => result.averageResult.selfAverage,
          result => result.effortResult.selfAverage,
          result => result.contributionResult.selfAverage,
          result => result.cooperationResult.selfAverage);

        this.setChartData(this.peerChartData,
          result => result.averageResult.peerAverage,
          result => result.effortResult.peerAverage,
          result => result.contributionResult.peerAverage,
          result => result.cooperationResult.peerAverage);


      } else {
        this.toastr.warning("Could not retrieve project team assment status. Message: " + (result.message || "unknown error"), "API error");
      }
    });

  }

  private setChartData(chartData: ChartData<'bar', number[]>,
    selectAverageScore: (result: IAssessmentResultModel) => number,
    selectEffortScore: (result: IAssessmentResultModel) => number,
    selectContributionScore: (result: IAssessmentResultModel) => number,
    selectCooperationScore: (result: IAssessmentResultModel) => number) {
    chartData.datasets = []
    chartData.datasets.push({
      label: 'Average',
      data: this.results.map(result => selectAverageScore(result))
    });
    chartData.datasets.push({
      label: 'Effort',
      barPercentage: 0.2,
      data: this.results.map(result => selectEffortScore(result))
    });
    chartData.datasets.push({
      label: 'Actual contribution',
      barPercentage: 0.2,
      data: this.results.map(result => selectContributionScore(result))
    });
    chartData.datasets.push({
      label: 'Cooperation',
      barPercentage: 0.2,
      data: this.results.map(result => selectCooperationScore(result))
    });
    chartData.labels = this.results.map(result => result.subject.fullName);
  } 

  public calculateIndividualGrades(): void{
    this.teamGrade = Math.max(this.teamGrade || 0, 0);
    this.results.forEach(result => {
      let indivdualGrade = this.teamGrade * (result.averageResult.average / result.averageResult.teamAverage);
      indivdualGrade = Math.min(indivdualGrade, 20);
      result.individualGrade = +indivdualGrade.toFixed(2);
    });
  }

}
