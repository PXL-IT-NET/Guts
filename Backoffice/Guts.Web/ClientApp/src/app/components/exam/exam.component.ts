import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { ExamService } from "../../services/exam.service";
import { AuthService } from "../../services/auth.service";
import { ActivatedRoute } from "@angular/router";
import { UserProfile } from "../../viewmodels/user.model";
import { ExamModel, ExamPartModel } from "../../viewmodels/exam.model";
import { Subscription, Subject } from "rxjs";
import { ToastrService } from "ngx-toastr";

@Component({
  standalone: false,
  selector: "app-exam",
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: "./exam.component.html",
})
export class ExamComponent implements OnInit, OnDestroy, OnChanges {
  private destroyRef = inject(DestroyRef);
  public loading: boolean = false;
  public userProfile: UserProfile;
  public exams: ExamModel[];
  public newExam: ExamModel;

  @Input() public courseId: number;

  private userProfileSubscription: Subscription;

  constructor(
    private route: ActivatedRoute,
    private examService: ExamService,
    private authService: AuthService,
    private toastr: ToastrService,
  ) {
    this.exams = [];
    this.newExam = new ExamModel();
    this.courseId = 0;
  }

  ngOnInit() {
    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService
      .getUserProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((profile) => {
        this.userProfile = profile;
      });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.courseId > 0) {
      this.loadExams();
    } else {
      this.exams = [];
    }
  }

  ngOnDestroy(): void {}

  public loadExams() {
    this.loading = true;
    this.examService
      .getExamsOfCourse(this.courseId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading = false;
        if (result.success) {
          this.exams = result.value;
        } else {
          this.toastr.error(
            "Could not retrieve exams from API. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  public saveNewExam() {
    this.loading = true;
    this.newExam.courseId = this.courseId;
    this.examService
      .saveExam(this.newExam)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading = false;
        if (result.success) {
          this.exams.push(result.value);
          this.newExam = new ExamModel();
        } else {
          this.toastr.error(
            "Could not save exam. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  public onExamPartDeleted(examPart: ExamPartModel, exam: ExamModel) {
    exam.parts.splice(exam.parts.indexOf(examPart), 1);
  }

  public onExamPartAdded(examPart: ExamPartModel, exam: ExamModel) {
    exam.parts.push(examPart);
  }

  public downloadResults(exam: ExamModel) {
    this.loading = true;
    this.examService
      .getExamResultsDownloadUrl(exam.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.loading = false;
        if (!result.success) {
          this.toastr.error(
            "Could not download results. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }
}
