import {
  Component,
  OnInit,
  OnDestroy,
  Input,
  OnChanges,
  SimpleChanges,
  ChangeDetectorRef,
} from "@angular/core";
import { ExamService } from "../../services/exam.service";
import { AuthService } from "../../services/auth.service";
import { ActivatedRoute } from "@angular/router";
import { UserProfile } from "../../viewmodels/user.model";
import { ExamModel, ExamPartModel } from "../../viewmodels/exam.model";
import { Subscription, Subject } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  selector: "app-exam",
  templateUrl: "./exam.component.html",
})
export class ExamComponent implements OnInit, OnDestroy, OnChanges {
  private destroy$ = new Subject<void>();
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
    private cdr: ChangeDetectorRef,
  ) {
    this.exams = [];
    this.newExam = new ExamModel();
    this.courseId = 0;
  }

  ngOnInit() {
    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService
      .getUserProfile()
      .pipe(takeUntil(this.destroy$))
      .subscribe((profile) => {
        this.userProfile = profile;

        this.cdr.detectChanges();
      });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.courseId > 0) {
      this.loadExams();
    } else {
      this.exams = [];
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    this.userProfileSubscription?.unsubscribe();
  }

  public loadExams() {
    this.loading = true;
    this.examService
      .getExamsOfCourse(this.courseId)
      .pipe(takeUntil(this.destroy$))
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

        this.cdr.detectChanges();
      });
  }

  public saveNewExam() {
    this.loading = true;
    this.newExam.courseId = this.courseId;
    this.examService
      .saveExam(this.newExam)
      .pipe(takeUntil(this.destroy$))
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

        this.cdr.detectChanges();
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
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        this.loading = false;
        if (!result.success) {
          this.toastr.error(
            "Could not download results. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }

        this.cdr.detectChanges();
      });
  }
}
