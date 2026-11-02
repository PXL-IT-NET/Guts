import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  Component,
  OnInit,
  OnDestroy,
  ChangeDetectionStrategy,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ChartData, ChartOptions } from "chart.js";
import { ToastrService } from "ngx-toastr";
import { forkJoin, of } from "rxjs";
import { catchError, map } from "rxjs/operators";
import { ProjectService, ProjectTeamAssessmentService } from "src/app/services";
import { IAssessmentResultModel } from "src/app/viewmodels/projectassessment.model";
import { ITeamModel } from "src/app/viewmodels/team.model";

@Component({
  standalone: false,
  selector: "app-project-team-assessment-detailed-results",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-team-assessment-detailed-results.component.html",
})
export class ProjectTeamAssessmentDetailedResultsComponent
  implements OnInit, OnDestroy
{
  private readonly destroyRef = inject(DestroyRef);

  private readonly _assessmentDescription = signal<string>("");
  public get assessmentDescription(): string {
    return this._assessmentDescription();
  }
  public set assessmentDescription(v: string) {
    this._assessmentDescription.set(v);
  }

  private readonly _allTeams = signal<ITeamModel[]>([]);
  public get allTeams(): ITeamModel[] {
    return this._allTeams();
  }
  public set allTeams(v: ITeamModel[]) {
    this._allTeams.set(v);
  }

  private readonly _completedTeams = signal<ITeamModel[]>([]);
  public get completedTeams(): ITeamModel[] {
    return this._completedTeams();
  }
  public set completedTeams(v: ITeamModel[]) {
    this._completedTeams.set(v);
  }

  private readonly _currentTeam = signal<ITeamModel>({ id: 0, name: "" });
  public get currentTeam(): ITeamModel {
    return this._currentTeam();
  }
  public set currentTeam(v: ITeamModel) {
    this._currentTeam.set(v);
  }

  private readonly _previousTeam = signal<ITeamModel | null>(null);
  public get previousTeam(): ITeamModel | null {
    return this._previousTeam();
  }
  public set previousTeam(v: ITeamModel | null) {
    this._previousTeam.set(v);
  }

  private readonly _nextTeam = signal<ITeamModel | null>(null);
  public get nextTeam(): ITeamModel | null {
    return this._nextTeam();
  }
  public set nextTeam(v: ITeamModel | null) {
    this._nextTeam.set(v);
  }

  private readonly _results = signal<IAssessmentResultModel[]>([]);
  public get results(): IAssessmentResultModel[] {
    return this._results();
  }
  public set results(v: IAssessmentResultModel[]) {
    this._results.set(v);
  }

  private readonly _teamGrade = signal<number>(0);
  public get teamGrade(): number {
    return this._teamGrade();
  }
  public set teamGrade(v: number) {
    this._teamGrade.set(v);
  }

  private readonly _gradeCorrectionType = signal<GradeCorrectionType>(
    GradeCorrectionType.Peers,
  );
  public get gradeCorrectionType(): GradeCorrectionType {
    return this._gradeCorrectionType();
  }
  public set gradeCorrectionType(v: GradeCorrectionType) {
    this._gradeCorrectionType.set(v);
  }

  private readonly _selfChartData = signal<ChartData<"bar", number[]>>({
    datasets: [],
    labels: [],
  });
  public get selfChartData(): ChartData<"bar", number[]> {
    return this._selfChartData();
  }
  public set selfChartData(v: ChartData<"bar", number[]>) {
    this._selfChartData.set(v);
  }

  private readonly _peerChartData = signal<ChartData<"bar", number[]>>({
    datasets: [],
    labels: [],
  });
  public get peerChartData(): ChartData<"bar", number[]> {
    return this._peerChartData();
  }
  public set peerChartData(v: ChartData<"bar", number[]>) {
    this._peerChartData.set(v);
  }

  private readonly _peerAndSelfChartData = signal<ChartData<"bar", number[]>>({
    datasets: [],
    labels: [],
  });
  public get peerAndSelfChartData(): ChartData<"bar", number[]> {
    return this._peerAndSelfChartData();
  }
  public set peerAndSelfChartData(v: ChartData<"bar", number[]>) {
    this._peerAndSelfChartData.set(v);
  }

  private readonly _peersVersusSelfChartData = signal<
    ChartData<"radar", number[]>
  >({ datasets: [], labels: [] });
  public get peersVersusSelfChartData(): ChartData<"radar", number[]> {
    return this._peersVersusSelfChartData();
  }
  public set peersVersusSelfChartData(v: ChartData<"radar", number[]>) {
    this._peersVersusSelfChartData.set(v);
  }

  public barChartOptions: ChartOptions<"bar"> = {
    scales: {
      y: {
        min: 0,
        max: 5,
        ticks: {
          callback: function (value, index, ticks) {
            if (value === 0) return "No contribution (0)";
            if (value === 1) return "Way below average (1)";
            if (value === 2) return "Below average (2)";
            if (value === 3) return "Average (3)";
            if (value === 4) return "Above average (4)";
            if (value === 5) return "Way above average (5)";
            return "";
          },
        },
        grid: {
          lineWidth: (ctx, options) => {
            if (ctx.tick.value === 3) {
              return 3; // set the dash pattern for the 0 gridline
            }
            return 1;
          },
        },
      },
    },
  };

  public radarChartOptions: ChartOptions<"radar"> = {
    scales: {
      r: {
        beginAtZero: true,
        suggestedMin: 0,
        suggestedMax: 5,
        ticks: {
          stepSize: 1,
        },
      },
    },
  };

  constructor(
    private readonly projectTeamAssessmentService: ProjectTeamAssessmentService,
    private readonly projectService: ProjectService,
    private readonly toastr: ToastrService,
    private readonly route: ActivatedRoute,
  ) {
    this.allTeams = [];
    this.completedTeams = [];
    this.currentTeam = {
      id: 0,
      name: "",
    };
    this.previousTeam = null;
    this.nextTeam = null;
    this.gradeCorrectionType = GradeCorrectionType.Peers;
    this.resetResults();
  }

  ngOnInit(): void {
    let courseId = +this.route.parent.snapshot.params["courseId"];
    let projectCode = this.route.snapshot.params["code"];
    let assessmentId = +this.route.snapshot.params["assessmentId"];

    this.route.params
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        let teamId = +params["teamId"];

        if (this.allTeams.length <= 0) {
          this.projectService
            .getProjectDetails(courseId, projectCode)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((result) => {
              if (result.success) {
                this.allTeams = result.value.teams;
                this.loadCompletedTeams(assessmentId, teamId);
              } else {
                this.toastr.error(
                  "Could not load project from API. Message: " +
                    (result.message || "unknown error"),
                  "System error",
                );
              }
            });
        } else {
          this.setTeams(teamId);
        }

        this.resetResults();
        this.projectTeamAssessmentService
          .getDetailedResultsOfProjectTeamAssessment(assessmentId, teamId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe((result) => {
            if (result.success) {
              this.results = result.value;
              if (this.results.length > 0 && this.results[0].assessmentDescription) {
                this.assessmentDescription = this.results[0].assessmentDescription;
              }
              this.calculateIndividualGrades();

              this.setChartData(
                this.peerAndSelfChartData,
                (result) => result.averageResult.value,
                (result) => result.effortResult.value,
                (result) => result.contributionResult.value,
                (result) => result.cooperationResult.value,
              );

              this.setChartData(
                this.selfChartData,
                (result) => result.averageResult.selfValue,
                (result) => result.effortResult.selfValue,
                (result) => result.contributionResult.selfValue,
                (result) => result.cooperationResult.selfValue,
              );

              this.setChartData(
                this.peerChartData,
                (result) => result.averageResult.peerValue,
                (result) => result.effortResult.peerValue,
                (result) => result.contributionResult.peerValue,
                (result) => result.cooperationResult.peerValue,
              );

              this.peersVersusSelfChartData.datasets.push(
                {
                  label: "Peers",
                  data: this.results.map(
                    (result) => result.averageResult.peerValue,
                  ),
                },
                {
                  label: "Self",
                  data: this.results.map(
                    (result) => result.averageResult.selfValue,
                  ),
                },
              );
              this.peersVersusSelfChartData.labels = this.results.map(
                (result) => result.subject.fullName,
              );
              this.peersVersusSelfChartData = {
                ...this.peersVersusSelfChartData,
              };
            } else {
              this.toastr.warning(
                "Could not retrieve project team assment results. Message: " +
                  (result.message || "unknown error"),
                "Warning",
              );
              this.resetResults();
            }
          });
      });
  }

  ngOnDestroy(): void {
    this.projectTeamAssessmentService.activeResultTeamName.set("");
  }

  public loadCompletedTeams(assessmentId: number, currentTeamId: number) {
    if (this.allTeams.length === 0) {
      this.completedTeams = [];
      this.setTeams(currentTeamId);
      return;
    }

    const statusObservables = this.allTeams.map((team) =>
      this.projectTeamAssessmentService
        .getStatusOfProjectTeamAssessment(assessmentId, team.id)
        .pipe(
          map((res) => ({
            team,
            isComplete: res.success && res.value?.isComplete === true,
          })),
          catchError(() => of({ team, isComplete: false })),
        ),
    );

    forkJoin(statusObservables)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((statuses) => {
        this.completedTeams = statuses
          .filter((s) => s.isComplete)
          .map((s) => s.team);
        this.setTeams(currentTeamId);
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

  public setTeams(currentTeamId: number) {
    this.currentTeam =
      this.allTeams.find((team) => team.id === currentTeamId) ?? {
        id: 0,
        name: "",
      };

    if (this.currentTeam?.name) {
      this.projectTeamAssessmentService.activeResultTeamName.set(
        this.currentTeam.name,
      );
    }

    const currentAllTeamsIndex = this.allTeams.findIndex(
      (team) => team.id === currentTeamId,
    );

    if (this.completedTeams.length > 0 && currentAllTeamsIndex !== -1) {
      const completedBefore = this.completedTeams.filter((t) => {
        const idx = this.allTeams.findIndex((team) => team.id === t.id);
        return idx !== -1 && idx < currentAllTeamsIndex;
      });
      this.previousTeam =
        completedBefore.length > 0
          ? (completedBefore.at(-1) ?? null)
          : null;

      const completedAfter = this.completedTeams.filter((t) => {
        const idx = this.allTeams.findIndex((team) => team.id === t.id);
        return idx !== -1 && idx > currentAllTeamsIndex;
      });
      this.nextTeam =
        completedAfter.length > 0 ? completedAfter[0] : null;
    } else {
      let currentIndex = this.allTeams.indexOf(this.currentTeam);
      this.previousTeam =
        currentIndex > 0 ? this.allTeams[currentIndex - 1] : null;
      this.nextTeam =
        currentIndex < this.allTeams.length - 1 && currentIndex >= 0
          ? this.allTeams[currentIndex + 1]
          : null;
    }
  }

  private setChartData(
    chartData: ChartData<"bar", number[]>,
    selectAverageScore: (result: IAssessmentResultModel) => number,
    selectEffortScore: (result: IAssessmentResultModel) => number,
    selectContributionScore: (result: IAssessmentResultModel) => number,
    selectCooperationScore: (result: IAssessmentResultModel) => number,
  ) {
    chartData.datasets = [];
    chartData.datasets.push(
      {
        label: "Average",
        data: this.results.map((result) => selectAverageScore(result)),
      },
      {
        label: "Effort",
        barPercentage: 0.2,
        data: this.results.map((result) => selectEffortScore(result)),
      },
      {
        label: "Actual contribution",
        barPercentage: 0.2,
        data: this.results.map((result) => selectContributionScore(result)),
      },
      {
        label: "Cooperation",
        barPercentage: 0.2,
        data: this.results.map((result) => selectCooperationScore(result)),
      },
    );
    chartData.labels = this.results.map((result) => result.subject.fullName);
  }

  public calculateIndividualGrades(): void {
    this.teamGrade = Math.max(this.teamGrade || 0, 0);
    this.results.forEach((result) => {
      let indivdualGrade = 0;
      switch (this.gradeCorrectionType) {
        case GradeCorrectionType.Peers: {
          indivdualGrade =
            this.teamGrade *
            (result.averageResult.peerValue /
              result.averageResult.averagePeerValue);
          break;
        }
        case GradeCorrectionType.Self: {
          indivdualGrade =
            this.teamGrade *
            (result.averageResult.selfValue /
              result.averageResult.averageSelfValue);
          break;
        }
        case GradeCorrectionType.PeersAndSelf: {
          indivdualGrade =
            this.teamGrade *
            (result.averageResult.value / result.averageResult.averageValue);
          break;
        }
      }
      indivdualGrade = Math.min(indivdualGrade, 20);
      result.individualGrade = +indivdualGrade.toFixed(2);
    });
    this._results.update((list) => [...list]);
  }
}

export enum GradeCorrectionType {
  Peers = 0,
  Self = 1,
  PeersAndSelf = 2,
}
