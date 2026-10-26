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
import { ProjectService } from "../../services";
import { CreateResult } from "../../util/result";
import { ToastrService } from "ngx-toastr";
import { IProjectDetailsModel } from "src/app/viewmodels/project.model";
import { ITopicAddModel } from "src/app/viewmodels/topic.model";

@Component({
  standalone: false,
  selector: "app-project-add",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project-add.component.html",
})
export class ProjectAddComponent implements OnInit {
  @Input() public courseId: number;
  @Input() public projectCode: string;

  @Output() public projectAdded: EventEmitter<IProjectDetailsModel>;

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public addProjectForm: FormGroup<{
    code: FormControl<string | null>;
    description: FormControl<string | null>;
  }>;

  constructor(
    public readonly modalRef: BsModalRef,
    private readonly projectService: ProjectService,
    private readonly toastr: ToastrService,
  ) {
    this.projectAdded = new EventEmitter<IProjectDetailsModel>();
  }

  ngOnInit() {
    this.addProjectForm = new FormGroup({
      code: new FormControl("", Validators.required),
      description: new FormControl("", Validators.required),
    });
  }

  onSubmit() {
    if (this.addProjectForm.invalid) return;

    const code: string = this.addProjectForm.controls.code.value || "";
    const description: string = this.addProjectForm.controls.description.value || "";
    this.addProject(code, description);
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }

  private addProject(code: string, description: string) {
    this.loading = true;
    const model: ITopicAddModel = {
      code: code,
      description: description,
    };
    this.projectService
      .addProject(this.courseId, model)
      .subscribe((result: CreateResult<IProjectDetailsModel>) => {
        this.loading = false;
        if (result.success) {
          this.modalRef.hide();
          this.projectAdded.emit(result.value);
        } else {
          this.toastr.error(
            result.message || "unknown error",
            "Could not add project",
          );
        }
      });
  }
}
