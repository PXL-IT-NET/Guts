import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, Input, OnInit, Optional, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import {
  ITeamDetailsModel,
  ITeamMemberModel,
} from "../../viewmodels/team.model";
import { BsModalRef } from "ngx-bootstrap/modal";
import {
  FormGroup,
  FormControl,
  Validators,
  AbstractControl,
} from "@angular/forms";
import { forkJoin, Observable } from "rxjs";
import { ProjectService, ProjectTeamAssessmentService } from "../../services";
import { ProjectAssessmentService } from "../../services/project.assessment.service";
import { PostResult } from "../../util/result";
import { ProjectAssessmentModel } from "../../viewmodels/projectassessment.model";
import { ToastrService } from "ngx-toastr";

@Component({
  standalone: false,
  selector: "app-project-team-edit",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-team-edit.component.html",
})
export class ProjectTeamEditComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  @Input() public courseId: number;
  @Input() public projectCode: string;

  private readonly _team = signal<ITeamDetailsModel>({} as ITeamDetailsModel);
  @Input()
  public get team(): ITeamDetailsModel {
    return this._team();
  }
  public set team(v: ITeamDetailsModel) {
    this._team.set(v);
  }

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private readonly _incompleteAssessments = signal<ProjectAssessmentModel[]>([]);
  public get incompleteAssessments(): ProjectAssessmentModel[] {
    return this._incompleteAssessments();
  }
  public set incompleteAssessments(v: ProjectAssessmentModel[]) {
    this._incompleteAssessments.set(v);
  }

  private readonly _hasPeerInclusionChanges = signal<boolean>(false);
  public get hasPeerInclusionChanges(): boolean {
    return this._hasPeerInclusionChanges();
  }

  private readonly initialPeerIdsByAssessment = new Map<number, number[]>();

  public editTeamForm: FormGroup<{
    name: FormControl<string | null>;
  }>;

  constructor(
    public readonly modalRef: BsModalRef,
    private readonly projectService: ProjectService,
    private readonly toastr: ToastrService,
    @Optional() private readonly projectAssessmentService?: ProjectAssessmentService,
    @Optional() private readonly projectTeamAssessmentService?: ProjectTeamAssessmentService,
  ) {}

  ngOnInit() {
    this.editTeamForm = new FormGroup({
      name: new FormControl<string | null>(null, Validators.required),
    });

    this.editTeamForm.patchValue(this.team);
    this.initialPeerIdsByAssessment.clear();
    this._hasPeerInclusionChanges.set(false);
    this.loadIncompleteAssessments();
  }

  private loadIncompleteAssessments() {
    if (
      !this.projectAssessmentService ||
      !this.projectTeamAssessmentService ||
      !this.courseId ||
      !this.projectCode ||
      !this.team?.id ||
      !this.team?.members?.length
    ) {
      return;
    }

    const projectDetails$ = this.projectService.getProjectDetails?.(
      this.courseId,
      this.projectCode,
    );
    if (!projectDetails$) {
      return;
    }

    projectDetails$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((projectResult) => {
        if (!projectResult?.success || !projectResult.value?.id) {
          return;
        }
        this.projectAssessmentService
          .getAssessmentsOfProject(projectResult.value.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe((assessmentsResult) => {
            if (!assessmentsResult?.success) {
              return;
            }
            const openedAssessments = assessmentsResult.value.filter(
              (a) => a.isAfterOpenOn,
            );
            this.incompleteAssessments = [];
            openedAssessments.forEach((assessment) => {
              this.refreshAssessmentStatus(assessment);
            });
          });
      });
  }

  private refreshAssessmentStatus(assessment: ProjectAssessmentModel) {
    if (!this.projectTeamAssessmentService || !this.team?.id) {
      return;
    }
    this.projectTeamAssessmentService
      .getStatusOfProjectTeamAssessment(assessment.id, this.team.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((statusResult) => {
        if (statusResult?.success) {
          assessment.teamStatus = statusResult.value;
          this.initialPeerIdsByAssessment.set(
            assessment.id,
            [...(statusResult.value.peerIds || [])],
          );
          this._incompleteAssessments.update((current) => {
            const withoutCurrent = current.filter((a) => a.id !== assessment.id);
            if (!assessment.teamStatus.isComplete) {
              return [...withoutCurrent, assessment].sort((a, b) => a.id - b.id);
            }
            return withoutCurrent;
          });
          this.updateHasPeerInclusionChanges();
        }
      });
  }

  public isPeerIncluded(
    assessment: ProjectAssessmentModel,
    member: ITeamMemberModel,
  ): boolean {
    return assessment.teamStatus?.peerIds?.includes(member.userId) ?? false;
  }

  public togglePeerInclusion(
    assessment: ProjectAssessmentModel,
    member: ITeamMemberModel,
    isIncluded: boolean,
  ) {
    if (!assessment.teamStatus) {
      return;
    }
    const currentPeerIds = assessment.teamStatus.peerIds || [];
    if (isIncluded && !currentPeerIds.includes(member.userId)) {
      assessment.teamStatus.peerIds = [...currentPeerIds, member.userId];
    } else if (!isIncluded) {
      assessment.teamStatus.peerIds = currentPeerIds.filter(
        (id) => id !== member.userId,
      );
    }
    this._incompleteAssessments.update((list) => [...list]);
    this.updateHasPeerInclusionChanges();
  }

  private updateHasPeerInclusionChanges() {
    let changed = false;
    for (const assessment of this.incompleteAssessments) {
      const initialPeerIds =
        this.initialPeerIdsByAssessment.get(assessment.id) || [];
      for (const member of this.team.members || []) {
        const initiallyIncluded = initialPeerIds.includes(member.userId);
        const currentlyIncluded = this.isPeerIncluded(assessment, member);
        if (initiallyIncluded !== currentlyIncluded) {
          changed = true;
          break;
        }
      }
      if (changed) {
        break;
      }
    }
    this._hasPeerInclusionChanges.set(changed);
  }

  onSubmit() {
    if (this.editTeamForm.invalid) return;

    const inclusionRequests: Observable<PostResult>[] = [];
    if (this.projectTeamAssessmentService) {
      for (const assessment of this.incompleteAssessments) {
        const initialPeerIds =
          this.initialPeerIdsByAssessment.get(assessment.id) || [];
        for (const member of this.team.members || []) {
          const initiallyIncluded = initialPeerIds.includes(member.userId);
          const currentlyIncluded = this.isPeerIncluded(assessment, member);
          if (initiallyIncluded !== currentlyIncluded) {
            inclusionRequests.push(
              this.projectTeamAssessmentService.setPeerInclusion(
                assessment.id,
                this.team.id,
                member.userId,
                currentlyIncluded,
              ),
            );
          }
        }
      }
    }

    const requests: Observable<PostResult>[] = [];
    if (!this.editTeamForm.pristine || inclusionRequests.length === 0) {
      //copy form values to team model
      Object.assign(this.team, this.editTeamForm.getRawValue());
      requests.push(
        this.projectService.updateTeam(
          this.courseId,
          this.projectCode,
          this.team,
        ),
      );
    }
    requests.push(...inclusionRequests);

    this.loading = true;
    forkJoin(requests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((results: PostResult[]) => {
        this.loading = false;
        const failedResult = results.find((r) => !r.success);
        if (!failedResult) {
          this.modalRef.hide();
        } else {
          this.toastr.error(
            failedResult.message || "unknown error",
            inclusionRequests.length > 0 && this.editTeamForm.pristine
              ? "Could not update peer inclusion"
              : "Could not update team",
          );
        }
      });
  }

  public removeFromTeam(member: ITeamMemberModel) {
    let confirmMessage =
      "Are you sure you want to remove '" +
      member.name +
      "' from '" +
      this.team.name +
      "'? All test results and incomplete peer assessments of this member will also be deleted.";
    if (confirm(confirmMessage)) {
      this.loading = true;
      this.projectService
        .removeFromTeam(
          this.courseId,
          this.projectCode,
          this.team.id,
          member.userId,
        )
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            //remove member from team
            this.team = {
              ...this.team,
              members: this.team.members.filter(
                (m) => m.userId !== member.userId,
              ),
            };
            this.incompleteAssessments.forEach((assessment) =>
              this.refreshAssessmentStatus(assessment),
            );
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Could not remove user from team",
            );
          }
        });
    }
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }
}
