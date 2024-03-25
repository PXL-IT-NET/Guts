import { Component, EventEmitter, Input, Output } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { FormGroup, FormControl, Validators, AbstractControl } from '@angular/forms';
import { ProjectAssessmentService } from '../../services';
import { PostResult } from '../../util/result';
import { ToastrService } from 'ngx-toastr';
import { ProjectAssessmentModel, ProjectAssessmentUpdateModel } from '../../viewmodels/projectassessment.model';

@Component({
  selector: 'app-project-assessment-edit',
  templateUrl: './project-assessment-edit.component.html'
})
export class ProjectAssessmentEditComponent {

  @Input() public projectId: number;
  @Input() public projectAssessment: ProjectAssessmentModel;

  @Output() public assessmentEdited: EventEmitter<void>;

  public loading: boolean;
  public editAssessmentForm: FormGroup;

  constructor(
    public modalRef: BsModalRef,
    private projectAssessmentService: ProjectAssessmentService,
    private toastr: ToastrService) {

    this.loading = false;
    this.assessmentEdited = new EventEmitter<void>();
  }

  ngOnInit() {
    this.editAssessmentForm = new FormGroup({
      'description': new FormControl('', Validators.required),
      'openOn': new FormControl('', Validators.required),
      'deadline': new FormControl('', Validators.required)
    });

    this.editAssessmentForm.get('description').setValue(this.projectAssessment.description);
    this.editAssessmentForm.get('openOn').setValue(this.projectAssessment.openOnUtc);
    this.editAssessmentForm.get('deadline').setValue(this.projectAssessment.deadlineUtc);
  }

  onSubmit() {
    if (this.editAssessmentForm.invalid) return;

    let model = new ProjectAssessmentUpdateModel();
    model.id = this.projectAssessment.id;
    Object.assign(model, this.editAssessmentForm.getRawValue());
    this.loading = true;
    this.projectAssessmentService.updateProjectAssessment(model).subscribe(
      (result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.assessmentEdited.emit();
          this.modalRef.hide();
        } else {
          this.toastr.error(result.message || "unknown error", "Could not update project assessment");
        }
      }
    );
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }
}
