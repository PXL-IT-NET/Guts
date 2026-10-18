import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import {
  AbstractControl,
  FormControl,
  FormGroup,
  Validators,
} from "@angular/forms";
import { BsModalRef } from "ngx-bootstrap/modal";
import { ToastrService } from "ngx-toastr";
import {
  ProjectAssessmentCreateModel,
  IProjectAssessmentModel,
} from "../../viewmodels/projectassessment.model";
import { ProjectAssessmentService } from "../../services/project.assessment.service";

@Component({
  standalone: false,
  selector: "app-project-assessment-add",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-assessment-add.component.html",
})
export class ProjectAssessmentAddComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public addAssessmentForm: FormGroup<{
    description: FormControl<string | null>;
    openOn: FormControl<any>;
    deadline: FormControl<any>;
  }>;

  @Input() public projectId: number;

  @Output() public assessmentAdded: EventEmitter<IProjectAssessmentModel>;

  constructor(
    public modalRef: BsModalRef,
    private projectAssessmentService: ProjectAssessmentService,
    private toastr: ToastrService,
  ) {
    this.assessmentAdded = new EventEmitter<IProjectAssessmentModel>();
  }

  ngOnInit() {
    this.addAssessmentForm = new FormGroup({
      description: new FormControl("", Validators.required),
      openOn: new FormControl<any>(null, Validators.required),
      deadline: new FormControl<any>(null, Validators.required),
    });
  }

  onSubmit() {
    this.addAssessmentForm.markAllAsTouched();
    if (this.addAssessmentForm.invalid) return;

    let model = new ProjectAssessmentCreateModel(this.projectId);
    Object.assign(model, this.addAssessmentForm.getRawValue());

    this.loading = true;
    this.projectAssessmentService
      .addProjectAssessment(model)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading = false;
        if (result.success) {
          this.assessmentAdded.emit(result.value);
          this.modalRef.hide();
        } else {
          this.toastr.error(
            "Could not add project peer assessment. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }

  ngOnDestroy(): void {}
}
