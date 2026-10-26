export interface ISolutionFileModel {
  filePath: string;
  content: string;
}

export class SolutionFileModel implements ISolutionFileModel {
  public filePath = "";
  public content = "";
  public isCollapsed = true;

  constructor(source?: ISolutionFileModel) {
    if (source) {
      this.filePath = source.filePath;
      this.content = source.content;
    }
  }
}
