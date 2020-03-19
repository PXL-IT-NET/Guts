export interface IUserModel {
  id: number;
  fullName: string;
}

export interface IUserProfile {
  id: number;
  roles: string[];
}

export class UserProfile implements IUserProfile {
  public id: number;
  public roles: string[];

  constructor(source?: IUserProfile) {
    this.id = 0;
    this.roles = [];

    if (source) {
      this.id = source.id;
      this.roles = source.roles;
    }
  }

  public isLector(): boolean {
    return this.roles.indexOf('lector') >= 0;
  }
}
