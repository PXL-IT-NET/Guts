import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, EventEmitter, Input, OnInit, Output, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import {
  ITeamDetailsModel,
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

@Component({
  standalone: false,
  selector: "app-project-team-add",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-team-add.component.html",
})
export class ProjectTeamAddComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  @Input() public courseId: number;
  @Input() public projectCode: string;

  @Output() public teamsAdded: EventEmitter<void>;

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public addTeamForm: FormGroup<{
    name: FormControl<string | null>;
    createMultipleTeams: FormControl<boolean | null>;
    teamNumberFrom: FormControl<number | null>;
    teamNumberTo: FormControl<number | null>;
  }>;

  constructor(
    public modalRef: BsModalRef,
    private projectService: ProjectService,
    private toastr: ToastrService,
  ) {
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

    const name: string = this.addTeamForm.controls.name.value || "";
    const createMultipleTeams: boolean =
      this.addTeamForm.controls.createMultipleTeams.value || false;
    const teamNumberFrom: number =
      this.addTeamForm.controls.teamNumberFrom.value || 1;
    const teamNumberTo: number = this.addTeamForm.controls.teamNumberTo.value || 1;

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
      .pipe(takeUntilDestroyed(this.destroyRef))
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
      });
  }

  private addSingleTeam(name: string) {
    this.loading = true;
    this.projectService
      .addTeam(this.courseId, this.projectCode, name)
      .pipe(takeUntilDestroyed(this.destroyRef))
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
      });
  }


}
