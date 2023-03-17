export interface IUserModel {
  id: number;
  fullName: string;
}

export interface IUserProfile {
  id: number;
  roles: string[];
  teams: number[];
}

export class UserProfile implements IUserProfile {
  public id: number;
  public roles: string[];
  public teams: number[];

  constructor(source?: IUserProfile) {
    this.id = 0;
    this.roles = [];
    this.teams = [];

    if (source) {
      this.id = source.id;
      this.roles = source.roles;
      this.teams = source.teams
    }
  }

  public isLector(): boolean {
    return this.roles.indexOf('lector') >= 0;
  }

  public isMemberOf(teamId: number): boolean {
    return this.teams.indexOf(teamId) >= 0;
  }
}
