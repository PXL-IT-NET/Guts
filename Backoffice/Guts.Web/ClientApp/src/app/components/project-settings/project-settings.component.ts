import { Component } from "@angular/core";
import { AbstractControl, FormControl, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ProjectService } from "src/app/services";
import { ITopicUpdateModel } from "src/app/viewmodels/topic.model";

@Component({
  selector: "app-project-settings",
  templateUrl: "./project-settings.component.html",
})
export class ProjectSettingsComponent {
  
  public loading: boolean;
  public editProjectForm: FormGroup;
  private courseId: number;
  private projectCode: string;

  constructor(
    private projectService: ProjectService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {
    this.loading = false;
    this.courseId = 0;
    this.projectCode = "";
  }

  ngOnInit() {
    this.editProjectForm = new FormGroup({
      code: new FormControl("", Validators.required),
      description: new FormControl("", Validators.required)
    });

    this.courseId = +this.route.parent.snapshot.params['courseId']
    this.projectCode = this.route.snapshot.params['code'];

    this.loadProject();
  }

  private loadProject() {

    this.loading = true;
    this.projectService.getProjectDetails(this.courseId, this.projectCode).subscribe({
      next: result => {
        this.loading = false;
        if (result.success) {
          this.editProjectForm.controls.code.setValue(result.value.code);
          this.editProjectForm.controls.description.setValue(result.value.description);
        } else {
          this.toastr.error("Could not load project. Message: " + (result.message || "unknown error"), "System error");
        }
      }
    });
  }

  onSubmit() {
    if (this.editProjectForm.invalid) return;

    const model: ITopicUpdateModel = {
      description: this.editProjectForm.controls.description.value
    };

    this.projectService.updateProject(this.courseId, this.projectCode, model).subscribe({
      next: result => {
        if (result.success) {
          this.toastr.success("Project updated successfully");
        } else {
          this.toastr.error(result.message || "unknown error", "Could not update project");
        }
      }
    });  
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }
}
