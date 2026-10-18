import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ChangeDetectionStrategy,
  signal
} from "@angular/core";
import { BsModalRef } from "ngx-bootstrap/modal";
import {
  FormGroup,
  FormControl,
  Validators,
  AbstractControl,
} from "@angular/forms";
import { ProjectAssessmentService } from "../../services";
import { PostResult } from "../../util/result";
import { ToastrService } from "ngx-toastr";
import {
  ProjectAssessmentModel,
  ProjectAssessmentUpdateModel,
} from "../../viewmodels/projectassessment.model";

@Component({
  standalone: false,
  selector: "app-project-assessment-edit",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-assessment-edit.component.html",
})
export class ProjectAssessmentEditComponent implements OnInit {
  @Input() public projectId: number;
  @Input() public projectAssessment: ProjectAssessmentModel;

  @Output() public assessmentEdited: EventEmitter<void>;

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public editAssessmentForm: FormGroup<{
    description: FormControl<string | null>;
    openOn: FormControl<any>;
    deadline: FormControl<any>;
  }>;

  constructor(
    public modalRef: BsModalRef,
    private projectAssessmentService: ProjectAssessmentService,
    private toastr: ToastrService,
  ) {
    this.assessmentEdited = new EventEmitter<void>();
  }

  ngOnInit() {
    this.editAssessmentForm = new FormGroup({
      description: new FormControl(this.projectAssessment?.description || "", Validators.required),
      openOn: new FormControl(this.projectAssessment?.openOnUtc || "", Validators.required),
      deadline: new FormControl(this.projectAssessment?.deadlineUtc || "", Validators.required),
    });
  }

  onSubmit() {
    if (this.editAssessmentForm.invalid) return;

    let model = new ProjectAssessmentUpdateModel();
    model.id = this.projectAssessment.id;
    Object.assign(model, this.editAssessmentForm.getRawValue());
    this.loading = true;
    this.projectAssessmentService
      .updateProjectAssessment(model)
      .subscribe((result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.assessmentEdited.emit();
          this.modalRef.hide();
        } else {
          this.toastr.error(
            result.message || "unknown error",
            "Could not update project assessment",
          );
        }
      });
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }
}
