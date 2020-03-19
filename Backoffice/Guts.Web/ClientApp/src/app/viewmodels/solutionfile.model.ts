export interface ISolutionFileModel {
    filePath: string;
    content: string;
}

export class SolutionFileModel implements ISolutionFileModel {
    public filePath: string;
    public content: string;  
    public isCollapsed: boolean;
  
    constructor(source?: ISolutionFileModel) {
      this.filePath = '';
      this.content = '';
      this.isCollapsed = true;
  
      if (source) {
        this.filePath = source.filePath;
        this.content = source.content;
      }
    }
  }
  