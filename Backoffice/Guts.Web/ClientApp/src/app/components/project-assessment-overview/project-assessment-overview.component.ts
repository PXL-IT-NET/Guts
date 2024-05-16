import { Component, OnInit } from '@angular/core';
import { UntypedFormBuilder, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { Subscription } from 'rxjs';
import { AuthService, ProjectService, ProjectTeamAssessmentService } from 'src/app/services';
import { ProjectAssessmentService } from 'src/app/services/project.assessment.service';
import { IProjectDetailsModel } from 'src/app/viewmodels/project.model';
import { IProjectAssessmentModel, ProjectAssessmentCreateModel, ProjectAssessmentModel } from 'src/app/viewmodels/projectassessment.model';
import { UserProfile } from 'src/app/viewmodels/user.model';
import { ProjectAssessmentAddComponent } from '../project-assessment-add/project-assessment-add.component';
import { ProjectAssessmentEditComponent } from '../project-assessment-edit/project-assessment-edit.component';
import { PostResult } from '../../util/result';

@Component({
  selector: 'app-project-assessment-overview',
  templateUrl: './project-assessment-overview.component.html'
})
export class ProjectAssessmentOverviewComponent implements OnInit {
  private userProfileSubscription: Subscription;

  public loading: boolean;
  public project: IProjectDetailsModel;
  public assessments: ProjectAssessmentModel[];
  public selectedTeamId: number;
  public userProfile: UserProfile;

  public modalRef: BsModalRef;

  //#Form
  public assessmentForm: UntypedFormGroup;

  public get descriptionControl() { return <UntypedFormControl>this.assessmentForm.get('description'); }
  public get openOnControl() { return <UntypedFormControl>this.assessmentForm.get('openOn'); }
  public get deadlineControl() { return <UntypedFormControl>this.assessmentForm.get('deadline'); }
  //#End Form


  constructor(private formBuilder: UntypedFormBuilder,
    private projectService: ProjectService,
    private projectAssessmentService: ProjectAssessmentService,
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private modalService: BsModalService,
    private authService: AuthService,
    private toastr: ToastrService,
    private route: ActivatedRoute) {
    this.loading = false;
    this.project = {
      id: 0,
      code: '',
      description: '',
      components: [],
      teams: [],
      assignments: []
    };

    this.assessments = [];
    this.selectedTeamId = 0;
    this.userProfile = new UserProfile();
  }

  ngOnInit(): void {

    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
    });

    //#Form
    this.assessmentForm = this.formBuilder.group({
      'description': [null, [Validators.required]],
      'openOn': [null, [Validators.required]],
      'deadline': [null, [Validators.required]]
    });
    //#End Form

    let courseId = +this.route.parent.snapshot.params['courseId']
    let projectCode = this.route.snapshot.params['code'];
    this.projectService.getProjectDetails(courseId, projectCode).subscribe(result => {
      if (result.success) {
        this.project = result.value;
        if (this.project.teams.length > 0) {
          this.selectedTeamId = this.project.teams[0].id;
        }
        this.loadProjectAssessments();
      } else {
        this.toastr.error("Could not load project from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }

  ngOnDestroy() {
    this.userProfileSubscription.unsubscribe();
  }

  public openAssessmentAddModal() {
    let modalState: ModalOptions = {
      initialState: {
        projectId: this.project.id,
      }
    };
    this.modalRef = this.modalService.show(ProjectAssessmentAddComponent, modalState);
    this.modalRef.setClass('modal-lg');
    this.modalRef.content.assessmentAdded.subscribe((addedAssessment) => {
      this.loadProjectAssessments();
    });
  }

  public openAssessmentEditModal(assessment: ProjectAssessmentModel) {
    let modalState: ModalOptions = {
      initialState: {
        projectId: this.project.id,
        projectAssessment: assessment
      }
    };
    this.modalRef = this.modalService.show(ProjectAssessmentEditComponent, modalState);
    this.modalRef.setClass('modal-lg');
    this.modalRef.content.assessmentEdited.subscribe(() => {
      this.loadProjectAssessments();
    });
  }

  public loadProjectAssessments() {
    this.loading = true;
    this.projectAssessmentService.getAssessmentsOfProject(this.project.id).subscribe(result => {
      this.loading = false;
      if (result.success) {
        this.assessments = result.value;
        if (this.selectedTeamId > 0) {
          this.assessments.forEach(assessment => {
            if (assessment.isAfterOpenOn) {
              this.projectTeamAssessmentService.getStatusOfProjectTeamAssessment(assessment.id, this.selectedTeamId).subscribe(result => {
                if (result.success) {
                  assessment.teamStatus = result.value;
                } else {
                  this.toastr.warning("Could not retrieve project team assment status. Message: " + (result.message || "unknown error"), "API error");
                }
              });
            }
          });
        }
        else {
          this.toastr.warning("You are not a member of a team. Please join a team first.", "No team found");
        }
      } else {
        this.toastr.error("Could not load project assessments from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }

  public deleteAssessment(assessment: ProjectAssessmentModel) {
    let confirmMessage = "Are you sure you want to remove '" + assessment.description + "'? This will also delete all team assessments of this project assessment...";
    if (confirm(confirmMessage)) {
      this.loading = true;
      this.projectAssessmentService.deleteProjectAssessment(assessment.id)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.toastr.success("Peer assessment successfully deleted");
            this.loadProjectAssessments();
          } else {
            this.toastr.error(result.message || "unknown error", "Could not delete peer assessment");
          }
        });
    }
  }

}
