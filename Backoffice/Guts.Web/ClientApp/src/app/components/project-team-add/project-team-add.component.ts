import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
} from "@angular/core";
import {
  ITeamDetailsModel,
  ITeamMemberModel,
  TeamGenerationModel,
} from "../../viewmodels/team.model";
import { BsModalRef } from "ngx-bootstrap/modal";
import {
  FormGroup,
  FormControl,
  Validators,
  AbstractControl,
} from "@angular/forms";
import { ProjectService } from "../../services";
import { CreateResult, PostResult } from "../../util/result";
import { ToastrService } from "ngx-toastr";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  selector: "app-project-team-add",
  templateUrl: "./project-team-add.component.html",
})
export class ProjectTeamAddComponent implements OnDestroy {
  private destroy$ = new Subject<void>();
  @Input() public courseId: number;
  @Input() public projectCode: string;

  @Output() public teamsAdded: EventEmitter<void>;

  public loading: boolean;
  public addTeamForm: FormGroup;

  constructor(
    public modalRef: BsModalRef,
    private projectService: ProjectService,
    private toastr: ToastrService,
    private cdr: ChangeDetectorRef,
  ) {
    this.loading = false;
    this.teamsAdded = new EventEmitter<void>();
  }

  ngOnInit() {
    this.addTeamForm = new FormGroup({
      name: new FormControl("", Validators.required),
      createMultipleTeams: new FormControl(false),
      teamNumberFrom: new FormControl(1, Validators.min(1)),
      teamNumberTo: new FormControl(1, Validators.min(1)),
    });
  }

  onSubmit() {
    if (this.addTeamForm.invalid) return;

    const name: string = this.addTeamForm.controls.name.value;
    const createMultipleTeams: boolean =
      this.addTeamForm.controls.createMultipleTeams.value;
    const teamNumberFrom: number =
      this.addTeamForm.controls.teamNumberFrom.value;
    const teamNumberTo: number = this.addTeamForm.controls.teamNumberTo.value;

    if (createMultipleTeams) {
      this.addMultipleTeams(name, teamNumberFrom, teamNumberTo);
    } else {
      this.addSingleTeam(name);
    }
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }

  private addMultipleTeams(baseName: string, from: number, to: number) {
    const model = new TeamGenerationModel(baseName, from, to);

    this.loading = true;
    this.projectService
      .generateTeams(this.courseId, this.projectCode, model)
      .pipe(takeUntil(this.destroy$))
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.modalRef.hide();
          this.teamsAdded.emit();
        } else {
          this.toastr.error(
            result.message || "unknown error",
            "Could not generate teams",
          );
        }

        this.cdr.detectChanges();
      });
  }

  private addSingleTeam(name: string) {
    this.loading = true;
    this.projectService
      .addTeam(this.courseId, this.projectCode, name)
      .pipe(takeUntil(this.destroy$))
      .subscribe((result: CreateResult<ITeamDetailsModel>) => {
        this.loading = false;
        if (result.success) {
          this.modalRef.hide();
          this.teamsAdded.emit();
        } else {
          this.toastr.error(
            result.message || "unknown error",
            "Could not generate teams",
          );
        }

        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
