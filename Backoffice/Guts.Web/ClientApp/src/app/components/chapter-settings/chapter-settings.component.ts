import { Component, OnInit, ChangeDetectionStrategy, signal } from "@angular/core";
import {
  AbstractControl,
  FormControl,
  FormGroup,
  Validators,
} from "@angular/forms";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ChapterService } from "src/app/services";
import {
  IAssignmentModel,
} from "src/app/viewmodels/assignment.model";
import { IChapterDetailsModel } from "src/app/viewmodels/chapter.model";
import { ITopicUpdateModel } from "src/app/viewmodels/topic.model";

@Component({
  standalone: false,
  selector: "app-chapter-settings",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./chapter-settings.component.html",
})
export class ChapterSettingsComponent implements OnInit {
  private courseId = 0;
  private chapterCode = "";

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public editChapterForm: FormGroup<{
    code: FormControl<string | null>;
    description: FormControl<string | null>;
  }>;

  private readonly _chapter = signal<IChapterDetailsModel>({
    id: 0,
    code: "",
    description: "",
    exercises: [],
    assignments: [],
    users: [],
  });
  public get chapter(): IChapterDetailsModel {
    return this._chapter();
  }
  public set chapter(v: IChapterDetailsModel) {
    this._chapter.set(v);
  }

  constructor(
    private readonly chapterService: ChapterService,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService,
  ) {}

  ngOnInit() {
    this.editChapterForm = new FormGroup({
      code: new FormControl("", Validators.required),
      description: new FormControl("", Validators.required),
    });

    this.courseId = +this.route.parent.snapshot.params["courseId"];
    this.chapterCode = this.route.snapshot.params["chapterCode"];

    this.loadChapter();
  }

  private loadChapter() {
    this.loading = true;
    this.chapterService
      .getChapterDetails(this.courseId, this.chapterCode)
      .subscribe({
        next: (result) => {
          this.loading = false;
          if (result.success) {
            this.chapter = result.value;
            this.editChapterForm.controls.code.setValue(result.value.code);
            this.editChapterForm.controls.description.setValue(
              result.value.description,
            );
          } else {
            this.toastr.error(
              "Could not load chapter. Message: " +
                (result.message || "unknown error"),
              "System error",
            );
          }
        },
      });
  }

  onSubmit() {
    if (this.editChapterForm.invalid) return;

    const model: ITopicUpdateModel = {
      description: this.editChapterForm.controls.description.value || "",
    };

    this.chapterService
      .updateChapter(this.courseId, this.chapter.code, model)
      .subscribe({
        next: (result) => {
          if (result.success) {
            this.toastr.success("Chapter updated successfully");
          } else {
            this.toastr.error(
              result.message || "unknown error",
              "Could not update chapter",
            );
          }
        },
      });
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }

  public onAssignmentChanged(assignment: IAssignmentModel) {
    this.loadChapter();
  }
}
