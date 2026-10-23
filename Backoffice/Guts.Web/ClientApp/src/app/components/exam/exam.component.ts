import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  Component,
  OnInit,

  Input,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./exam.component.html",
})
export class ExamComponent implements OnInit, OnChanges {
  private destroyRef = inject(DestroyRef);

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private _exams = signal<ExamModel[]>([]);
  public get exams(): ExamModel[] {
    return this._exams();
  }
  public set exams(v: ExamModel[]) {
    this._exams.set(v);
  }

  private _newExam = signal<ExamModel>(new ExamModel());
  public get newExam(): ExamModel {
    return this._newExam();
  }
  public set newExam(v: ExamModel) {
    this._newExam.set(v);
  }

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
          this.exams = [...this.exams, result.value];
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
    this._exams.update((exams) => [...exams]);
  }

  public onExamPartAdded(examPart: ExamPartModel, exam: ExamModel) {
    exam.parts.push(examPart);
    this._exams.update((exams) => [...exams]);
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
