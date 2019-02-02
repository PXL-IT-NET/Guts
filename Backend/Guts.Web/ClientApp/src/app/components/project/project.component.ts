import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IProjectDetailsModel } from '../../viewmodels/project.model';
import { TopicContextProvider, TopicContext } from "../../services/topic.context.provider";
import { ProjectService } from '../../services/project.service';
import { GetResult } from "../../util/Result";
import { ToastrService } from 'ngx-toastr';
import * as moment from 'moment';

@Component({
  templateUrl: './project.component.html'
})
export class ProjectComponent implements OnInit, OnDestroy {

  public model: IProjectDetailsModel;
  public context: TopicContext;
  public selectedDate: Date;
  public selectedAssignmentId: number;
  public selectedTeamId: number;
  public datePickerSettings: any;
  public loading: boolean = false;

  private courseId: number;
  private projectCode: string;

  constructor(private projectService: ProjectService, 
    private topicContextProvider: TopicContextProvider,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService) {
    this.courseId = 0;
    this.projectCode = '';

    this.model = {
      id: 0,
      code: '',
      description: '',
      components: [],
      teams: []
    };

    this.selectedAssignmentId = 0;
    this.selectedDate = new Date();
    this.selectedTeamId = 0;
    this.datePickerSettings = {
      bigBanner: true,
      timePicker: true,
      format: 'dd-MM-yyyy HH:mm'
    };
  }

  ngOnInit() {
    this.route.params.subscribe(params => {

      var parentParams = this.route.parent.snapshot.params;
      this.courseId = +parentParams['courseId']; // (+) converts 'courseId' to a number
      this.projectCode = params['code'];

      this.loading = true;
      this.projectService.getProjectDetails(this.courseId, this.projectCode).subscribe((result: GetResult<IProjectDetailsModel>) => {
        this.loading = false;
        if (result.success) {
          this.model = result.value;
          this.selectedAssignmentId = 0;

          if (this.model.teams.length <= 0) {
            this.navigateToTeamOverview();
          } else {
            this.selectedTeamId = this.model.teams[0].id;
            this.navigateToSummaryForSelectedTeam();
            this.loadStatistics();
          }

        } else {
          this.toastr.error("Could not load project details from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });

    });
  }

  ngOnDestroy() {

  }

  public onSelectionChanged() {
    if (this.selectedAssignmentId > 0) {
      this.router.navigate(['teams', this.selectedTeamId, 'components', this.selectedAssignmentId], { relativeTo: this.route });
    } else {
      this.navigateToSummaryForSelectedTeam();
    }
  }

  public onDateChanged() {
    this.topicContextProvider.setTopic(this.courseId, this.model, moment(this.selectedDate));
    this.loadStatistics();
  }

  private navigateToTeamOverview() {
    this.router.navigate(['teams'], { relativeTo: this.route }).then(() => {
      this.topicContextProvider.setTopic(this.courseId, this.model, moment(this.selectedDate));
    });
  }

  private navigateToSummaryForSelectedTeam() {
    this.topicContextProvider.setTopic(this.courseId, this.model, moment(this.selectedDate));
    this.router.navigate(['teams', this.selectedTeamId, 'summary'], { relativeTo: this.route });
  }

  private loadStatistics() {
    this.projectService.getProjectStatistics(this.courseId, this.projectCode, moment(this.selectedDate))
      .subscribe((result) => {
        if (result.success) {
          this.topicContextProvider.setStatistics(result.value);
        } else {
          this.toastr.error("Could not load project statistics from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
  }
}
