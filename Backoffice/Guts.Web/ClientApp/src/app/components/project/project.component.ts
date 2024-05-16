import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IProjectDetailsModel } from '../../viewmodels/project.model';
import { ProjectService } from '../../services/project.service';
import { GetResult } from "../../util/result";
import { ToastrService } from 'ngx-toastr';
import * as moment from 'moment';

@Component({
  templateUrl: './project.component.html'
})
export class ProjectComponent implements OnInit {

  public model: IProjectDetailsModel;
  public selectedDate: moment.Moment;
  public selectedAssignmentId: number;
  public selectedTeamId: number;
  public datePickerSettings: any;
  public loading: boolean = false;

  public courseId: number;
  public projectCode: string;

  constructor(private projectService: ProjectService, 
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
      teams: [],
      assignments: []
    };

    this.selectedAssignmentId = 0;
    this.selectedDate = moment();
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

          if (this.model.teams.length > 0) {
            this.selectedTeamId = this.model.teams[0].id;
          }

        } else {
          this.toastr.error("Could not load project details from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });

    });

     // Subscribe to queryParams to detect changes in query string parameters
     this.route.queryParams.subscribe((queryParams) => {
      if (queryParams["assignmentId"]) {
        this.selectedAssignmentId = +queryParams["assignmentId"];
      } else{
        this.selectedAssignmentId = 0;
      }
      
    });
  }
}