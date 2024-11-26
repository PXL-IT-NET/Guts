import { Component } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AssignmentService, ChapterService } from 'src/app/services';
import { TestService } from 'src/app/services/test.service';
import { PostResult } from 'src/app/util/result';
import { IAssignmentModel, ITestModel } from 'src/app/viewmodels/assignment.model';
import { IChapterDetailsModel } from 'src/app/viewmodels/chapter.model';
import { ITopicUpdateModel } from 'src/app/viewmodels/topic.model';

@Component({
  selector: 'app-chapter-settings',
  templateUrl: './chapter-settings.component.html'
})
export class ChapterSettingsComponent {
  private courseId: number;
  private chapterCode: string;


  public loading: boolean;
  public editChapterForm: FormGroup;

  public chapter: IChapterDetailsModel;

  constructor(
    private chapterService: ChapterService,
    private testService: TestService,
    private assignmentService: AssignmentService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {
    this.loading = false;
    this.courseId = 0;
    this.chapterCode = "";
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
    this.chapterCode = this.route.snapshot.params['chapterCode'];

    this.loadChapter();
  }

  private loadChapter() {

    this.loading = true;
    this.chapterService.getChapterDetails(this.courseId, this.chapterCode).subscribe({
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

  public deleteExercise(exercise: IAssignmentModel) {
    let confirmMessage = "Are you sure you want to remove '" + exercise.description + "'? All tests and test results will also be deleted.";
    if (confirm(confirmMessage)) {
      this.assignmentService.deleteAssignment(exercise.assignmentId)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.toastr.info("The exercise '" + exercise.description + "' is succesfully deleted", "Exercise deleted");
            this.loadChapter();
          } else {
            this.toastr.error(result.message || "unknown error", "Could not delete exercise");
          }
        });
    }
  }

  public deleteTest(exercise: IAssignmentModel, test: ITestModel) {
    let confirmMessage = "Are you sure you want to remove the test '" + test.testName + "' from the exercise '" + exercise.description + "'? The linked test results will also be deleted.";
    if (confirm(confirmMessage)) {
      this.testService.deleteTest(test.id)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.toastr.info("The test '" + test.testName + "' is succesfully deleted", "Test deleted");
            this.loadChapter();
          } else {
            this.toastr.error(result.message || "unknown error", "Could not delete test");
          }
        });
    }
  }
}
