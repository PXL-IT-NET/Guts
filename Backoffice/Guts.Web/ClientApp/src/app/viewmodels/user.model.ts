export interface IUserModel {
  id: number;
  fullName: string;
}

export interface IUserProfile {
  id: number;
  email: string;
  roles: string[];
  teams: number[];
  isAuthenticated: boolean;
}

export class UserProfile implements IUserProfile {
  public id: number;
  public email: string;
  public roles: string[];
  public teams: number[];
  public isAuthenticated: boolean;

  constructor(source?: IUserProfile) {
    this.id = 0;
    this.roles = [];
    this.teams = [];
    this.email = '';
    this.isAuthenticated = false;

    if (source) {
      this.id = source.id;
      this.email = source.email;
      this.roles = source.roles;
      this.teams = source.teams;
      this.isAuthenticated = true;
    }
  }

  public isLector(): boolean {
    return this.roles.indexOf('lector') >= 0;
  }

  public isMemberOf(teamId: number): boolean {
    return this.teams.indexOf(teamId) >= 0;
  }
}
