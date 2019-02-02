import { Component, OnInit, OnDestroy } from '@angular/core';
import { ProjectService } from '../../services/project.service';
import { TopicContextProvider } from "../../services/topic.context.provider";
import { TopicStatisticsModel, TopicSummaryModel } from '../../viewmodels/topic.model';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

@Component({
  templateUrl: './projectsummary.component.html'
})
export class ProjectSummaryComponent implements OnInit, OnDestroy {
  public model: TopicSummaryModel;
  public statistics: TopicStatisticsModel;
  public loadingSummary: boolean = false;
  public loadingStatistics: boolean = false;

  private teamId: number;
  private projectContextSubscription: Subscription;
  private projectStatisticsSubscription: Subscription;

  constructor(private projectService: ProjectService,
    private topicContextProvider: TopicContextProvider,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
    this.model = {
      id: 0,
      code: '',
      description: '',
      assignmentSummaries: [],
    };
    this.statistics = {
      id: 0,
      code: '',
      description: '',
      assignmentStatistics: []
    };
    this.teamId = 0;

    this.route.params.subscribe(params => {
      this.teamId = +params['teamId']; // (+) converts 'userId' to a number
      this.loadProjectSummary();
    });

    this.projectContextSubscription = this.topicContextProvider.topicChanged$.subscribe(() => {
      this.loadProjectSummary();
    });

    this.loadingStatistics = true;
    this.projectStatisticsSubscription = this.topicContextProvider.statisticsChanged$.subscribe(() => {
      this.loadingStatistics = false;
      this.statistics = this.topicContextProvider.currentContext.statistics;
    });
  }

  ngOnInit() {

  }

  ngOnDestroy() {
    this.projectContextSubscription.unsubscribe();
    this.projectStatisticsSubscription.unsubscribe();
    this.statistics = {
      id: 0,
      code: '',
      description: '',
      assignmentStatistics: []
    };
  }

  private loadProjectSummary() {
    if (!this.topicContextProvider.currentContext.topic) return;

    this.loadingSummary = true;
    this.projectService.getProjectSummary(this.topicContextProvider.currentContext.courseId,
      this.topicContextProvider.currentContext.topic.code,
      this.teamId,
      this.topicContextProvider.currentContext.statusDate)
      .subscribe((result) => {
        this.loadingSummary = false;

        if (result.success) {
          this.model = result.value;
        } else {
          this.toastr.error("Could not load project summary from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
  }
}
