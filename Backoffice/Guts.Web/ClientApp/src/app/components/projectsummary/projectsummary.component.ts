import { Component, Input, SimpleChanges } from '@angular/core';
import { ProjectService } from '../../services/project.service';
import { TopicStatisticsModel, TopicSummaryModel } from '../../viewmodels/topic.model';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import * as moment from "moment";

@Component({
  selector: 'app-project-summary',
  templateUrl: './projectsummary.component.html'
})
export class ProjectSummaryComponent {
  public model: TopicSummaryModel;
  public statistics: TopicStatisticsModel;
  public loadingSummary: boolean = false;
  public loadingStatistics: boolean = false;

  @Input() public courseId: number;
  @Input() public teamId: number;
  @Input() public projectCode: string;
  @Input() public statusDate: moment.Moment;

  constructor(private projectService: ProjectService,
    private toastr: ToastrService) {
    this.model = new TopicSummaryModel();
    this.statistics = {
      id: 0,
      code: '',
      description: '',
      assignmentStatistics: []
    };

    this.courseId = 0;
    this.teamId = 0;
    this.projectCode = '';
    this.statusDate = moment();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.courseId <= 0) return;
    let projectHasValue: boolean = this.projectCode && this.projectCode.length > 0;
    if (!projectHasValue) return;
    let projectHasChanged: boolean = (changes.projectCode ?? false) && changes.projectCode.previousValue != this.projectCode;
    let statusDateHasChanged: boolean = (changes.statusDate ?? false) && changes.statusDate.previousValue != this.statusDate;

    if (projectHasChanged || (statusDateHasChanged && projectHasValue)) {
      this.loadProjectStatistics();
    }

    if (this.teamId <= 0) return;
    this.loadProjectSummary();
  }

  private loadProjectSummary() {
    this.loadingSummary = true;
    this.projectService.getProjectSummary(this.courseId,
      this.projectCode,
      this.teamId,
      this.statusDate)
      .subscribe((result) => {
        this.loadingSummary = false;
        if (result.success) {
          this.model = result.value;
        } else {
          this.toastr.error("Could not load project summary from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
  }

  private loadProjectStatistics() {
    this.loadingStatistics = true;
    this.projectService.getProjectStatistics(this.courseId, this.projectCode, this.statusDate).subscribe((result) => {
      if (result.success) {
        this.statistics = result.value;
      } else {
        this.toastr.error("Could not load project statistics from API. Message: " + (result.message || "unknown error"), "API error");
      }
      this.loadingStatistics = false;
    });
  }
}
