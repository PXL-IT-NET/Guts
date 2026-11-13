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
  isImpersonating?: boolean;
}

export class UserProfile implements IUserProfile {
  public id = 0;
  public email = "";
  public roles: string[] = [];
  public teams: number[] = [];
  public isAuthenticated = false;
  public isImpersonating = false;

  constructor(source?: IUserProfile) {
    if (source) {
      this.id = source.id;
      this.email = source.email;
      this.roles = source.roles;
      this.teams = source.teams;
      this.isAuthenticated = true;
      this.isImpersonating = Boolean(source.isImpersonating);
    }
  }

  public isLector(): boolean {
    return this.roles.includes("lector");
  }

  public isTeacher(): boolean {
    return this.isLector();
  }

  public isAdmin(): boolean {
    return this.roles.includes("admin");
  }

  public isMemberOf(teamId: number): boolean {
    return this.teams.includes(teamId);
  }
}
