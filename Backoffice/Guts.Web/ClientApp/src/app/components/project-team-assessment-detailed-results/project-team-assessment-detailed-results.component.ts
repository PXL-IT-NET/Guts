import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ChartData, ChartOptions, ScriptableScaleContext } from 'chart.js';
import { ToastrService } from 'ngx-toastr';
import { ProjectService, ProjectTeamAssessmentService } from 'src/app/services';
import { IAssessmentResultModel } from 'src/app/viewmodels/projectassessment.model';
import { ITeamModel } from 'src/app/viewmodels/team.model';

@Component({
  selector: 'app-project-team-assessment-detailed-results',
  templateUrl: './project-team-assessment-detailed-results.component.html'
})
export class ProjectTeamAssessmentDetailedResultsComponent implements OnInit {

  public allTeams: ITeamModel[];
  public currentTeam: ITeamModel;
  public previousTeam: ITeamModel;
  public nextTeam: ITeamModel;

  public results: IAssessmentResultModel[];
  public teamGrade: number;
  public gradeCorrectionType: GradeCorrectionType;

  public selfChartData: ChartData<'bar', number[]>;
  public peerChartData: ChartData<'bar', number[]>;
  public peerAndSelfChartData: ChartData<'bar', number[]>;
  public peersVersusSelfChartData: ChartData<'radar', number[]>;

  public barChartOptions: ChartOptions<'bar'> = {
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
        },
        grid: {
          lineWidth: (ctx, options) => {
            if (ctx.tick.value === 3) {
              return 3; // set the dash pattern for the 0 gridline
            }
            return 1;
          }
        }
      }
    }
  };

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
    this.allTeams = [];
    this.currentTeam = {
      id: 0,
      name: ''
    };
    this.previousTeam = null;
    this.nextTeam = null;
    this.gradeCorrectionType = GradeCorrectionType.Peers;
    this.resetResults();
  }

  ngOnInit(): void {

    let courseId = +this.route.parent.snapshot.params['courseId']
    let projectCode = this.route.snapshot.params['code'];
    let assessmentId = this.route.snapshot.params['assessmentId'];

    this.route.params.subscribe(params => {
      let teamId = params['teamId'];

      if (this.allTeams.length <= 0) {
        this.projectService.getProjectDetails(courseId, projectCode).subscribe(result => {
          if (result.success) {
            this.allTeams = result.value.teams;
            this.setTeams(teamId);

          } else {
            this.toastr.error("Could not load project from API. Message: " + (result.message || "unknown error"), "System error");
          }
        });
      } else {
        this.setTeams(teamId);
      }

      this.resetResults();
      this.projectTeamAssessmentService.getDetailedResultsOfProjectTeamAssessment(assessmentId, teamId).subscribe(result => {
        if (result.success) {
          this.results = result.value;
          this.calculateIndividualGrades();

          this.setChartData(this.peerAndSelfChartData,
            result => result.averageResult.value,
            result => result.effortResult.value,
            result => result.contributionResult.value,
            result => result.cooperationResult.value);

          this.setChartData(this.selfChartData,
            result => result.averageResult.selfValue,
            result => result.effortResult.selfValue,
            result => result.contributionResult.selfValue,
            result => result.cooperationResult.selfValue);

          this.setChartData(this.peerChartData,
            result => result.averageResult.peerValue,
            result => result.effortResult.peerValue,
            result => result.contributionResult.peerValue,
            result => result.cooperationResult.peerValue);

          this.peersVersusSelfChartData.datasets.push({
            label: 'Peers',
            data: this.results.map(result => result.averageResult.peerValue)
          });
          this.peersVersusSelfChartData.datasets.push({
            label: 'Self',
            data: this.results.map(result => result.averageResult.selfValue)
          });
          this.peersVersusSelfChartData.labels = this.results.map(result => result.subject.fullName);
        } else {
          this.toastr.warning("Could not retrieve project team assment results. Message: " + (result.message || "unknown error"), "Warning");
          this.resetResults();
        }
      });
    });
  }

  private resetResults() {
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

    this.peersVersusSelfChartData = {
      datasets: [],
      labels: [],
    };
  }

  private setTeams(currentTeamId: number) {
    this.currentTeam = this.allTeams.find(team => team.id == currentTeamId);
    let currentIndex = this.allTeams.indexOf(this.currentTeam);

    if (currentIndex > 0) {
      this.previousTeam = this.allTeams[currentIndex - 1];
    } else {
      this.previousTeam = null;
    }

    if (currentIndex < this.allTeams.length - 1) {
      this.nextTeam = this.allTeams[currentIndex + 1];
    } else {
      this.nextTeam = null;
    }
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

  public calculateIndividualGrades(): void {
    this.teamGrade = Math.max(this.teamGrade || 0, 0);
    this.results.forEach(result => {
      let indivdualGrade = 0;
      switch (this.gradeCorrectionType) {
        case GradeCorrectionType.Peers: {
          indivdualGrade = this.teamGrade * (result.averageResult.peerValue / result.averageResult.averagePeerValue);
          break;
        }
        case GradeCorrectionType.Self: {
          indivdualGrade = this.teamGrade * (result.averageResult.selfValue / result.averageResult.averageSelfValue);
          break;
        }
        case GradeCorrectionType.PeersAndSelf: {
          indivdualGrade = this.teamGrade * (result.averageResult.value / result.averageResult.averageValue);
          break;
        }
      }
      indivdualGrade = Math.min(indivdualGrade, 20);
      result.individualGrade = +indivdualGrade.toFixed(2);
    });
  }
}

export enum GradeCorrectionType {
  Peers = 0,
  Self = 1,
  PeersAndSelf = 2
}
