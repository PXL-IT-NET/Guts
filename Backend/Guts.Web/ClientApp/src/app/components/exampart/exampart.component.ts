import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { ExamPartModel, AssignmentEvaluation } from '../../viewmodels/exam.model';
import { AssignmentService, ExamService } from 'src/app/services';
import { ITopicAssignmentModel } from 'src/app/viewmodels/assignment.model';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-exampart',
  templateUrl: './exampart.component.html',
})
export class ExampartComponent implements OnInit {

  @Input() public model: ExamPartModel;
  @Input() public examId: number;
  @Output() public examPartAdded = new EventEmitter<ExamPartModel>();

  public allAssignments: ITopicAssignmentModel[];
  public newAssigmnentEvaluation: AssignmentEvaluation;
  public isCollapsed: boolean;

  private courseId: number;

  constructor(private route: ActivatedRoute,
    private assignmentService: AssignmentService,
    private examService: ExamService,
    private toastr: ToastrService) {
    this.isCollapsed = true;
    this.courseId = 0;
  }

  ngOnInit() {

    this.route.params.subscribe(params => {
      this.courseId = +params['courseId']; // (+) converts 'courseId' to a number

      this.assignmentService.getAssignmentsOfCourse(this.courseId).subscribe(result => {
        if (result.success) {
          this.allAssignments = result.value;
        } else {
          this.toastr.error("Could not retrieve assignments from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });

    });

    if (!this.model) {
      this.model = new ExamPartModel();
    }

    this.newAssigmnentEvaluation = new AssignmentEvaluation();
  }

  public addAssignment() {
    //TODO: do some validations

    this.model.assignmentEvaluations.push(this.newAssigmnentEvaluation);
    this.newAssigmnentEvaluation = new AssignmentEvaluation();
  }

  public saveExampart() {
    this.examService.saveExamPart(this.examId, this.model).subscribe(
      result => {
        if (result.success) {
          this.examPartAdded.emit(result.value);
          this.model = new ExamPartModel();
        } else {
          this.toastr.error("Could not save exam part. Message: " + (result.message || "unknown error"), "API error");
        }
      }
    );
  }

  public getAssignmentName(assignmentId: number) {
    var assignment = this.allAssignments.find(a => a.assignmentId == assignmentId);
    if (!assignment) return '???';
    return assignment.topicDescription + ' - ' + (assignment.description || assignment.code);
  }
}
