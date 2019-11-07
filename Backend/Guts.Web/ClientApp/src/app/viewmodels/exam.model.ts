import { IDateTimeModel } from "./datetime.model"

export interface IExamModel {
  id: number;
  courseId: number;
  name: string;
  maximumScore: number;
  parts : IExamPartModel[];
}

export interface IExamPartModel {
  id: number;
  name: string;
  deadline: IDateTimeModel;
  assignmentEvaluations: IAssignmentEvaluation[];
}

export interface IAssignmentEvaluation {
  id: number;
  assignmentId: number;
  maximumScore: number;
  numberOfTestsAlreadyGreenAtStart: number;
}

export class ExamPartModel implements IExamPartModel {
  public id: number;  
  public name: string;
  public deadline: IDateTimeModel;
  public assignmentEvaluations: IAssignmentEvaluation[];

  constructor(){
    this.id = 0;
    this.name = '';
    this.deadline = {
      year: 0,
      month: 0,
      day: 0,
      hour: 0,
      minute: 0,
      second: 0
    };
    this.assignmentEvaluations = [];
  }
}

export class ExamModel implements IExamModel {
  public id: number;
  public courseId: number;
  public name: string;
  public maximumScore: number;
  public parts: IExamPartModel[];

  //UI
  public isCollapsed: boolean;

  constructor(source?: IExamModel) {
    this.id = 0;
    this.courseId = 0;
    this.name = '';
    this.maximumScore =  0;
    this.parts = [];
    this.isCollapsed = true;

    if (source) {
      this.id = source.id;
      this.courseId = source.courseId;
      this.name = source.name;
      this.maximumScore = source.maximumScore;
      this.parts = source.parts;
    }
  }
}
