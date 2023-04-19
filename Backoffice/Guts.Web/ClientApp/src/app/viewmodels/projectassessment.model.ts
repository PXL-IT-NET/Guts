import * as moment from "moment";

export class ProjectAssessmentCreateModel {
  projectId: number;
  description: string;
  openOn: moment.Moment;
  deadline: moment.Moment;

  constructor(projectId: number) {
    this.projectId = projectId;
    this.description = '';
    this.openOn = null;
    this.deadline = null;
  }
}

export interface IProjectAssessmentModel {
  id: number;
  description: string;
  openOnUtc: moment.Moment;
  deadlineUtc: moment.Moment;
  teamStatus: IProjectTeamAssessmentStatusModel;
}

export class ProjectAssessmentModel implements IProjectAssessmentModel {
  public id: number;
  public description: string;
  public openOnUtc: moment.Moment;
  public get openOnDisplay() : string {
    return this.openOnUtc.local().format("dddd, DD/MM/YYYY, HH:mm");
  }
  public get isOpen() : boolean {
    let now = moment();
    return now.isAfter(this.openOnUtc) && this.deadlineUtc.isAfter(now);
  }

  public deadlineUtc: moment.Moment;
  public get deadlineDisplay() : string {
    return this.deadlineUtc.local().format("dddd, DD/MM/YYYY, HH:mm");
  }
  public get isOver() : boolean {
    let now = moment();
    return now.isAfter(this.deadlineUtc);
  }

  public teamStatus: IProjectTeamAssessmentStatusModel;

  constructor(source?: IProjectAssessmentModel) {
    this.id = 0;
    this.description = '';
    this.openOnUtc = null;
    this.deadlineUtc = null;
    this.teamStatus = new ProjectTeamAssessmentStatusModel();

    if (source) {
      this.id = source.id;
      this.description = source.description;
      this.openOnUtc = moment.utc(source.openOnUtc);
      this.deadlineUtc = moment.utc(source.deadlineUtc);
    }
  }
}

export interface IProjectTeamAssessmentStatusModel {
  id: number;
  teamId: number;
  isComplete: boolean;
  peersThatNeedToEvaluateOthers: IUserModel[];
}

export class ProjectTeamAssessmentStatusModel implements IProjectTeamAssessmentStatusModel {
  public id: number;
  public teamId: number;
  public isComplete: boolean;
  public peersThatNeedToEvaluateOthers: IUserModel[];

  constructor(source?: IProjectTeamAssessmentStatusModel) {
    this.id = 0;
    this.teamId = 0;
    this.isComplete = false;
    this.peersThatNeedToEvaluateOthers = [];

    if (source) {
      Object.assign(this, source);
      this.peersThatNeedToEvaluateOthers = source.peersThatNeedToEvaluateOthers.map(peer => new UserModel(peer));
    }
  }
}

export interface IUserModel {
  id: number;
  fullName: string;
}

export class UserModel implements IUserModel {
  public id: number;
  public fullName: string;

  constructor(source?: IUserModel) {
    this.id = 0;
    this.fullName = '';

    if (source) {
      Object.assign(this, source);
    }
  }
}

export interface IPeerAssessmentModel{
  subject: IUserModel;
  user: IUserModel;
  contributionScore: number;
  cooperationScore: number;
  effortScore: number;
  isSelfAssessment: boolean;
  explanation: string;
}

export class PeerAssessmentModel implements IPeerAssessmentModel{
  public subject: IUserModel;
  public user: IUserModel;
  public contributionScore: number;
  public cooperationScore: number;
  public effortScore: number;
  public isSelfAssessment: boolean;
  public explanation: string;

  constructor(source?: IPeerAssessmentModel) {
    this.subject = new UserModel();
    this.user = new UserModel();
    this.contributionScore = 3;
    this.cooperationScore = 3;
    this.effortScore = 3;
    this.isSelfAssessment = false;
    this.explanation = '';

    if (source) {
      Object.assign(this, source);
      this.subject = new UserModel(source.subject);
      this.user = new UserModel(source.user);
    }
  }
}

export interface IAssessmentResultModel{
  subject: IUserModel;
  selfAssessment: IPeerAssessmentModel;
  peerAssessments: IPeerAssessmentModel[];
  averageResult: IAssessmentSubResultModel;
  effortResult: IAssessmentSubResultModel;
  cooperationResult: IAssessmentSubResultModel;
  contributionResult: IAssessmentSubResultModel;
  individualGrade: number;
}

export interface IAssessmentSubResultModel{
  value: number;
  selfValue: number;
  peerValue: number;

  score: number;
  selfScore: number;
  peerScore: number;

  averageValue: number;
  averageSelfValue: number;
  averagePeerValue: number;
}



