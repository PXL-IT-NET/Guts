import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import {
  UntypedFormBuilder,
  UntypedFormControl,
  UntypedFormGroup,
  Validators,
} from "@angular/forms";
import { ActivatedRoute } from "@angular/router";
import { BsModalRef, BsModalService, ModalOptions } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import { Subscription, Subject } from "rxjs";
import {
  AuthService,
  PeriodProvider,
  ProjectService,
  ProjectTeamAssessmentService,
} from "src/app/services";
import { ProjectAssessmentService } from "src/app/services/project.assessment.service";
import { IProjectDetailsModel } from "src/app/viewmodels/project.model";
import {
  IProjectAssessmentModel,
  ProjectAssessmentCreateModel,
  ProjectAssessmentModel,
} from "src/app/viewmodels/projectassessment.model";
import { UserProfile } from "src/app/viewmodels/user.model";
import { ProjectAssessmentAddComponent } from "../project-assessment-add/project-assessment-add.component";
import { ProjectAssessmentEditComponent } from "../project-assessment-edit/project-assessment-edit.component";
import { PostResult } from "../../util/result";

@Component({
  standalone: false,
  selector: "app-project-assessment-overview",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-assessment-overview.component.html",
})
export class ProjectAssessmentOverviewComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  private userProfileSubscription: Subscription;

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private _project = signal<IProjectDetailsModel>({
    id: 0,
    code: "",
    description: "",
    components: [],
    teams: [],
    assignments: [],
  });
  public get project(): IProjectDetailsModel {
    return this._project();
  }
  public set project(v: IProjectDetailsModel) {
    this._project.set(v);
  }

  private _assessments = signal<ProjectAssessmentModel[]>([]);
  public get assessments(): ProjectAssessmentModel[] {
    return this._assessments();
  }
  public set assessments(v: ProjectAssessmentModel[]) {
    this._assessments.set(v);
  }

  private _selectedTeamId = signal<number>(0);
  public get selectedTeamId(): number {
    return this._selectedTeamId();
  }
  public set selectedTeamId(v: number) {
    this._selectedTeamId.set(v);
  }

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private _activePeriod = signal<boolean>(true);
  public get activePeriod(): boolean {
    return this._activePeriod();
  }
  public set activePeriod(v: boolean) {
    this._activePeriod.set(v);
  }

  public modalRef: BsModalRef;

  //#Form
  public assessmentForm: UntypedFormGroup;

  public get descriptionControl() {
    return <UntypedFormControl>this.assessmentForm.get("description");
  }
  public get openOnControl() {
    return <UntypedFormControl>this.assessmentForm.get("openOn");
  }
  public get deadlineControl() {
    return <UntypedFormControl>this.assessmentForm.get("deadline");
  }
  //#End Form

  constructor(
    private formBuilder: UntypedFormBuilder,
    private projectService: ProjectService,
    private projectAssessmentService: ProjectAssessmentService,
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private modalService: BsModalService,
    private authService: AuthService,
    private toastr: ToastrService,
    private route: ActivatedRoute,
    private periodProvider: PeriodProvider,
  ) {
    this.loading = false;
    this.project = {
      id: 0,
      code: "",
      description: "",
      components: [],
      teams: [],
      assignments: [],
    };

    this.assessments = [];
    this.selectedTeamId = 0;
    this.userProfile = new UserProfile();
  }

  ngOnInit(): void {
    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService
      .getUserProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((profile) => {
        this.userProfile = profile;
      });

    //#Form
    this.assessmentForm = this.formBuilder.group({
      description: [null, [Validators.required]],
      openOn: [null, [Validators.required]],
      deadline: [null, [Validators.required]],
    });
    //#End Form

    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      let courseId = +this.route.parent.snapshot.params["courseId"];
      let projectCode = this.route.snapshot.params["code"];
      this.projectService
        .getProjectDetails(courseId, projectCode)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          if (result.success) {
            this.project = result.value;
            if (this.project.teams.length > 0) {
              this.selectedTeamId = this.project.teams[0].id;
            }
            this.loadProjectAssessments();
          } else {
            this.toastr.error(
              "Could not load project from API. Message: " +
                (result.message || "unknown error"),
              "System error",
            );
          }
        });
    });

    this.periodProvider.period$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((period) => {
        if (period) {
          this.activePeriod = period.isActive;
        }
      });
  }

  ngOnDestroy(): void {}

  public openAssessmentAddModal() {
    let modalState: ModalOptions = {
      initialState: {
        projectId: this.project.id,
      },
    };
    this.modalRef = this.modalService.show(
      ProjectAssessmentAddComponent,
      modalState,
    );
    this.modalRef.setClass("modal-lg");
    this.modalRef.content.assessmentAdded
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((addedAssessment) => {
        this.loadProjectAssessments();
      });
  }

  public openAssessmentEditModal(assessment: ProjectAssessmentModel) {
    let modalState: ModalOptions = {
      initialState: {
        projectId: this.project.id,
        projectAssessment: assessment,
      },
    };
    this.modalRef = this.modalService.show(
      ProjectAssessmentEditComponent,
      modalState,
    );
    this.modalRef.setClass("modal-lg");
    this.modalRef.content.assessmentEdited
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadProjectAssessments();
      });
  }

  public loadProjectAssessments() {
    this.loading = true;
    this.projectAssessmentService
      .getAssessmentsOfProject(this.project.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading = false;
        if (result.success) {
          this.assessments = result.value;
          if (this.selectedTeamId > 0) {
            this.assessments.forEach((assessment) => {
              if (assessment.isAfterOpenOn) {
                this.projectTeamAssessmentService
                  .getStatusOfProjectTeamAssessment(
                    assessment.id,
                    this.selectedTeamId,
                  )
                  .pipe(takeUntilDestroyed(this.destroyRef))
                  .subscribe((result) => {
                    if (result.success) {
                      assessment.teamStatus = result.value;
                      this._assessments.update((list) => [...list]);
                    } else {
                      this.toastr.warning(
                        "Could not retrieve project team assment status. Message: " +
                          (result.message || "unknown error"),
                        "Warning",
                      );
                    }
                  });
              }
            });
          } else {
            this.toastr.warning(
              "You are not a member of a team. Please join a team first.",
              "No team found",
            );
          }
        } else {
          this.toastr.error(
            "Could not load project assessments from API. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  public deleteAssessment(assessment: ProjectAssessmentModel) {
    let confirmMessage =
      "Are you sure you want to remove '" +
      assessment.description +
      "'? This will also delete all team assessments of this project assessment...";
    if (confirm(confirmMessage)) {
      this.loading = true;
      this.projectAssessmentService
        .deleteProjectAssessment(assessment.id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.toastr.success("Peer assessment successfully deleted");
            this.loadProjectAssessments();
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Could not delete peer assessment",
            );
          }
        });
    }
  }
}
