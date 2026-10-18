import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import {
  Component,
  OnInit,
  Input,
  Output,
  EventEmitter,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import {
  ExamPartModel,
  AssignmentEvaluation,
} from "../../viewmodels/exam.model";
import { AssignmentService, ExamService } from "src/app/services";
import { ITopicAssignmentModel } from "src/app/viewmodels/assignment.model";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import {
  UntypedFormGroup,
  UntypedFormControl,
  Validators,
} from "@angular/forms";

@Component({
  standalone: false,
  selector: "app-exampart",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./exampart.component.html",
})
export class ExampartComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef, { optional: true });

  @Input() public model: ExamPartModel;
  @Input() public examId: number;
  @Output() public examPartDeleted = new EventEmitter<ExamPartModel>();
  @Output() public examPartAdded = new EventEmitter<ExamPartModel>();

  private _allAssignments = signal<ITopicAssignmentModel[]>([]);
  public get allAssignments(): ITopicAssignmentModel[] {
    return this._allAssignments();
  }
  public set allAssignments(v: ITopicAssignmentModel[]) {
    this._allAssignments.set(v);
  }

  private _newAssigmnentEvaluation = signal<AssignmentEvaluation>(new AssignmentEvaluation());
  public get newAssigmnentEvaluation(): AssignmentEvaluation {
    return this._newAssigmnentEvaluation();
  }
  public set newAssigmnentEvaluation(v: AssignmentEvaluation) {
    this._newAssigmnentEvaluation.set(v);
  }

  private _isCollapsed = signal<boolean>(true);
  public get isCollapsed(): boolean {
    return this._isCollapsed();
  }
  public set isCollapsed(v: boolean) {
    this._isCollapsed.set(v);
  }

  public newAssignmentEvaluationForm: UntypedFormGroup;
  public get assignmentId() {
    return this.newAssignmentEvaluationForm.get("assignmentId");
  }
  public get maximumScore() {
    return this.newAssignmentEvaluationForm.get("maximumScore");
  }
  public get numberOfTestsAlreadyGreenAtStart() {
    return this.newAssignmentEvaluationForm.get(
      "numberOfTestsAlreadyGreenAtStart",
    );
  }

  private courseId: number;

  constructor(
    private route: ActivatedRoute,
    private assignmentService: AssignmentService,
    private examService: ExamService,
    private toastr: ToastrService,
  ) {
    this.isCollapsed = true;
    this.courseId = 0;
  }

  ngOnInit() {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.courseId = +params["courseId"]; // (+) converts 'courseId' to a number

      this.assignmentService
        .getAssignmentsOfCourse(this.courseId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          if (result.success) {
            this.allAssignments = result.value.sort(
              (a: ITopicAssignmentModel, b: ITopicAssignmentModel) => {
                var topicCompare = a.topicDescription.localeCompare(
                  b.topicDescription,
                );
                if (topicCompare !== 0) return topicCompare;
                var descriptionA = a.description || a.code;
                var descriptionB = b.description || b.code;
                return descriptionA.localeCompare(descriptionB);
              },
            );
          } else {
            this.toastr.error(
              "Could not retrieve assignments from API. Message: " +
                (result.message || "unknown error"),
              "System error",
            );
          }
        });
    });

    if (!this.model) {
      this.model = new ExamPartModel();
    }

    this.newAssigmnentEvaluation = new AssignmentEvaluation();

    this.newAssignmentEvaluationForm = new UntypedFormGroup({
      assignmentId: new UntypedFormControl(
        this.newAssigmnentEvaluation.assignmentId,
        [Validators.min(1)],
      ),
      maximumScore: new UntypedFormControl(
        this.newAssigmnentEvaluation.maximumScore,
        [Validators.required, Validators.min(1)],
      ),
      numberOfTestsAlreadyGreenAtStart: new UntypedFormControl(
        this.newAssigmnentEvaluation.numberOfTestsAlreadyGreenAtStart,
        [Validators.required, Validators.min(0)],
      ),
    });
  }

  public addAssignment() {
    this.model.assignmentEvaluations.push(this.newAssigmnentEvaluation);
    this.newAssigmnentEvaluation = new AssignmentEvaluation();
    this.cdr?.markForCheck();
  }

  public saveExampart() {
    if (this.model.assignmentEvaluations.length < 1) {
      this.toastr.error(
        "An exam part must have at least one assignment evaluation",
      );
      return;
    }

    this.examService
      .saveExamPart(this.examId, this.model)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.success) {
          this.examPartAdded.emit(result.value);
          this.model = new ExamPartModel();
          this.isCollapsed = true;
          this.cdr?.markForCheck();
        } else {
          this.toastr.error(
            "Could not save exam part. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  public deleteExampart() {
    this.examService
      .deleteExamPart(this.examId, this.model.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.success) {
          this.examPartDeleted.emit(this.model);
          this.cdr?.markForCheck();
        } else {
          this.toastr.error(
            "Could not delete exam part. Message: " +
              (result.message || "unknown error"),
            "System error",
          );
        }
      });
  }

  public getAssignmentName(assignmentId: number) {
    if (!this.allAssignments) return "???";
    var assignment = this.allAssignments.find(
      (a) => a.assignmentId == assignmentId,
    );
    if (!assignment) return "???";
    return (
      assignment.topicDescription +
      " - " +
      (assignment.description || assignment.code)
    );
  }
}
