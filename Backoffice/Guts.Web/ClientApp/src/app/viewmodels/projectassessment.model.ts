import { formatDateTime } from "../util/date.util";

export type AssessmentDate = Date | string | null;

export class ProjectAssessmentCreateModel {
  projectId: number;
  description = "";
  openOn: AssessmentDate = null;
  deadline: AssessmentDate = null;

  constructor(projectId: number) {
    this.projectId = projectId;
  }
}

export class ProjectAssessmentUpdateModel {
  id = 0;
  description = "";
  openOn: AssessmentDate = null;
  deadline: AssessmentDate = null;
}

export interface IProjectAssessmentModel {
  id: number;
  description: string;
  openOnUtc: Date | string | null;
  deadlineUtc: Date | string | null;
  teamStatus: IProjectTeamAssessmentStatusModel;
}

export class ProjectAssessmentModel implements IProjectAssessmentModel {
  public id = 0;
  public description = "";
  public openOnUtc: Date | null = null;
  public get openOnDisplay(): string {
    return formatDateTime(this.openOnUtc);
  }
  public get isOpen(): boolean {
    if (!this.openOnUtc || !this.deadlineUtc) return false;
    const now = Date.now();
    return (
      now > new Date(this.openOnUtc).getTime() &&
      new Date(this.deadlineUtc).getTime() > now
    );
  }

  public get isAfterOpenOn(): boolean {
    if (!this.openOnUtc) return false;
    return Date.now() > new Date(this.openOnUtc).getTime();
  }

  public deadlineUtc: Date | null = null;
  public get deadlineDisplay(): string {
    return formatDateTime(this.deadlineUtc);
  }
  public get isOver(): boolean {
    if (!this.deadlineUtc) return false;
    return Date.now() > new Date(this.deadlineUtc).getTime();
  }

  public teamStatus: IProjectTeamAssessmentStatusModel =
    new ProjectTeamAssessmentStatusModel();

  constructor(source?: IProjectAssessmentModel) {
    if (source) {
      this.id = source.id;
      this.description = source.description;
      this.openOnUtc = source.openOnUtc ? new Date(source.openOnUtc) : null;
      this.deadlineUtc = source.deadlineUtc
        ? new Date(source.deadlineUtc)
        : null;
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
  public id = 0;
  public teamId = 0;
  public isComplete = false;
  public peersThatNeedToEvaluateOthers: IUserModel[] = [];

  constructor(source?: IProjectTeamAssessmentStatusModel) {
    if (source) {
      Object.assign(this, source);
      this.peersThatNeedToEvaluateOthers =
        source.peersThatNeedToEvaluateOthers.map((peer) => new UserModel(peer));
    }
  }
}

export interface IUserModel {
  id: number;
  fullName: string;
}

export class UserModel implements IUserModel {
  public id = 0;
  public fullName = "";

  constructor(source?: IUserModel) {
    if (source) {
      Object.assign(this, source);
    }
  }
}

export interface IPeerAssessmentModel {
  subject: IUserModel;
  user: IUserModel;
  contributionScore: number;
  cooperationScore: number;
  effortScore: number;
  isSelfAssessment: boolean;
  explanation: string;
}

export class PeerAssessmentModel implements IPeerAssessmentModel {
  public subject: IUserModel = new UserModel();
  public user: IUserModel = new UserModel();
  public contributionScore = -1;
  public cooperationScore = -1;
  public effortScore = -1;
  public isSelfAssessment = false;
  public explanation = "";

  constructor(source?: IPeerAssessmentModel) {
    if (source) {
      Object.assign(this, source);
      this.subject = new UserModel(source.subject);
      this.user = new UserModel(source.user);
    }
  }
}

export interface IAssessmentResultModel {
  subject: IUserModel;
  selfAssessment: IPeerAssessmentModel;
  peerAssessments: IPeerAssessmentModel[];
  averageResult: IAssessmentSubResultModel;
  effortResult: IAssessmentSubResultModel;
  cooperationResult: IAssessmentSubResultModel;
  contributionResult: IAssessmentSubResultModel;
  individualGrade: number;
}

export interface IAssessmentSubResultModel {
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
