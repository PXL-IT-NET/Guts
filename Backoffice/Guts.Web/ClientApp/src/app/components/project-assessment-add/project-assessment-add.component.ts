import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { ProjectAssessmentCreateModel, IProjectAssessmentModel } from '../../viewmodels/projectassessment.model';
import { ProjectAssessmentService } from '../../services/project.assessment.service';

@Component({
  selector: 'app-project-assessment-add',
  templateUrl: './project-assessment-add.component.html'
})
export class ProjectAssessmentAddComponent {
  public loading: boolean;
  public addAssessmentForm: FormGroup;

  @Input() public projectId: number;

  @Output() public assessmentAdded: EventEmitter<IProjectAssessmentModel>;

  constructor(
    public modalRef: BsModalRef,
    private projectAssessmentService: ProjectAssessmentService,
    private toastr: ToastrService) {

    this.loading = false;
    this.assessmentAdded = new EventEmitter<IProjectAssessmentModel>();
  }

  ngOnInit() {
    this.addAssessmentForm = new FormGroup({
      'description': new FormControl('', Validators.required),
      'openOn': new FormControl('', Validators.required),
      'deadline': new FormControl('', Validators.required)
    });
  }

  onSubmit() {
    this.addAssessmentForm.markAllAsTouched();
    if (this.addAssessmentForm.invalid) return;

    let model = new ProjectAssessmentCreateModel(this.projectId);
    Object.assign(model, this.addAssessmentForm.getRawValue());

    this.loading = true;
    this.projectAssessmentService.addProjectAssessment(model).subscribe(result => {
      this.loading = false;
      if (result.success) {
        this.assessmentAdded.emit(result.value);
        this.modalRef.hide();
      } else {
        this.toastr.error("Could not add project peer assessment. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }
}
