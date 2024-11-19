import { Component } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ChapterService } from 'src/app/services';
import { IChapterDetailsModel } from 'src/app/viewmodels/chapter.model';
import { ITopicUpdateModel } from 'src/app/viewmodels/topic.model';

@Component({
  selector: 'app-chapter-settings',
  templateUrl: './chapter-settings.component.html'
})
export class ChapterSettingsComponent {
  public loading: boolean;
  public editChapterForm: FormGroup;
  private courseId: number;
  public chapter: IChapterDetailsModel;

  constructor(
    private chapterService: ChapterService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {
    this.loading = false;
    this.courseId = 0;
    this.chapter = {
      id: 0,
      code: "",
      description: "",
      exercises: [],
      assignments: [],
      users: []
    };
  }

  ngOnInit() {
    this.editChapterForm = new FormGroup({
      code: new FormControl("", Validators.required),
      description: new FormControl("", Validators.required)
    });

    this.courseId = +this.route.parent.snapshot.params['courseId']
    let chapterCode = this.route.snapshot.params['chapterCode'];

    this.loadChapter(chapterCode);
  }

  private loadChapter(chapterCode: string) {

    this.loading = true;
    this.chapterService.getChapterDetails(this.courseId, chapterCode).subscribe({
      next: result => {
        this.loading = false;
        if (result.success) {
          this.chapter = result.value;
          this.editChapterForm.controls.code.setValue(result.value.code);
          this.editChapterForm.controls.description.setValue(result.value.description);
        } else {
          this.toastr.error("Could not load chapter. Message: " + (result.message || "unknown error"), "System error");
        }
      }
    });
  }

  onSubmit() {
    if (this.editChapterForm.invalid) return;

    const model: ITopicUpdateModel = {
      description: this.editChapterForm.controls.description.value
    };

    this.chapterService.updateChapter(this.courseId, this.chapter.code, model).subscribe({
      next: result => {
        if (result.success) {
          this.toastr.success("Chapter updated successfully");
        } else {
          this.toastr.error(result.message || "unknown error", "Could not update chapter");
        }
      }
    });  
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }
}
