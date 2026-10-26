import { NgbTimeStruct, NgbDateStruct } from "@ng-bootstrap/ng-bootstrap";

export interface IExamModel {
  id: number;
  courseId: number;
  name: string;
  maximumScore: number;
  parts: IExamPartModel[];
}

export interface IExamPartModel {
  id: number;
  name: string;
  deadline: Date;
  assignmentEvaluations: IAssignmentEvaluation[];
}

export interface IAssignmentEvaluation {
  id: number;
  assignmentId: number;
  maximumScore: number;
  numberOfTestsAlreadyGreenAtStart: number;
}

export class AssignmentEvaluation implements IAssignmentEvaluation {
  public id = 0;
  public assignmentId = 0;
  public maximumScore: number = null;
  public numberOfTestsAlreadyGreenAtStart: number = null;

  constructor(source?: IAssignmentEvaluation) {
    if (source) {
      this.id = source.id;
      this.assignmentId = source.assignmentId;
      this.maximumScore = source.maximumScore;
      this.numberOfTestsAlreadyGreenAtStart =
        source.numberOfTestsAlreadyGreenAtStart;
    }
  }
}

export class ExamPartModel implements IExamPartModel {
  public id = 0;
  public name = "";
  public assignmentEvaluations: IAssignmentEvaluation[] = [];

  public deadlineDate: NgbDateStruct;
  public deadlineTime: NgbTimeStruct;
  public get deadline(): Date {
    return new Date(
      this.deadlineDate.year,
      this.deadlineDate.month - 1,
      this.deadlineDate.day,
      this.deadlineTime.hour,
      this.deadlineTime.minute,
      this.deadlineTime.second ?? 0,
    );
  }
  public set deadline(value: Date) {
    const d = new Date(value);
    this.deadlineDate = {
      year: d.getFullYear(),
      month: d.getMonth() + 1,
      day: d.getDate(),
    };
    this.deadlineTime = {
      hour: d.getHours(),
      minute: d.getMinutes(),
      second: 0,
    };
  }

  constructor(source?: IExamPartModel) {
    const startOfDay = new Date();
    startOfDay.setHours(0, 0, 0, 0);
    this.deadline = startOfDay;

    if (source) {
      this.id = source.id;
      this.name = source.name;
      this.deadline = source.deadline;
      this.assignmentEvaluations = source.assignmentEvaluations.map(
        (ae) => new AssignmentEvaluation(ae),
      );
    }
  }
}

export class ExamModel implements IExamModel {
  public id = 0;
  public courseId = 0;
  public name = "";
  public maximumScore = 0;
  public parts: ExamPartModel[] = [];

  //UI
  public isCollapsed = true;

  constructor(source?: IExamModel) {
    if (source) {
      this.id = source.id;
      this.courseId = source.courseId;
      this.name = source.name;
      this.maximumScore = source.maximumScore;
      this.parts = source.parts.map((p) => new ExamPartModel(p));
    }
  }
}
