import { Component, OnInit, ChangeDetectionStrategy, signal } from "@angular/core";
import {
  AbstractControl,
  FormControl,
  FormGroup,
  Validators,
} from "@angular/forms";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ProjectService } from "src/app/services";
import { IAssignmentModel } from "src/app/viewmodels/assignment.model";
import { IProjectDetailsModel } from "src/app/viewmodels/project.model";
import { ITopicUpdateModel } from "src/app/viewmodels/topic.model";

@Component({
  standalone: false,
  selector: "app-project-settings",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-settings.component.html",
})
export class ProjectSettingsComponent implements OnInit {
  private courseId: number;
  private projectCode: string;

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public editProjectForm: FormGroup<{
    code: FormControl<string | null>;
    description: FormControl<string | null>;
  }>;

  private readonly _project = signal<IProjectDetailsModel>({} as IProjectDetailsModel);
  public get project(): IProjectDetailsModel {
    return this._project();
  }
  public set project(v: IProjectDetailsModel) {
    this._project.set(v);
  }

  constructor(
    private readonly projectService: ProjectService,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService,
  ) {
    this.courseId = 0;
    this.projectCode = "";
  }

  ngOnInit() {
    this.editProjectForm = new FormGroup({
      code: new FormControl("", Validators.required),
      description: new FormControl("", Validators.required),
    });

    this.courseId = +this.route.parent.snapshot.params["courseId"];
    this.projectCode = this.route.snapshot.params["code"];

    this.loadProject();
  }

  private loadProject() {
    this.loading = true;
    this.projectService
      .getProjectDetails(this.courseId, this.projectCode)
      .subscribe({
        next: (result) => {
          this.loading = false;
          if (result.success) {
            this.project = result.value;
            this.editProjectForm.controls.code.setValue(result.value.code);
            this.editProjectForm.controls.description.setValue(
              result.value.description,
            );
          } else {
            this.toastr.error(
              "Could not load project. Message: " +
                (result.message || "unknown error"),
              "System error",
            );
          }
        },
      });
  }

  onSubmit() {
    if (this.editProjectForm.invalid) return;

    const model: ITopicUpdateModel = {
      description: this.editProjectForm.controls.description.value || "",
    };

    this.projectService
      .updateProject(this.courseId, this.projectCode, model)
      .subscribe({
        next: (result) => {
          if (result.success) {
            this.toastr.success("Project updated successfully");
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Could not update project",
            );
          }
        },
      });
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }

  public onAssignmentChanged(assignment: IAssignmentModel) {
    this.loadProject();
  }
}
