export interface IPeriodModel {
  id: number;
  description: string;
  from: Date | string | null;
  until: Date | string | null;
  isActive: boolean;
}

export class PeriodModel implements IPeriodModel {
  public id = 0;
  public description = "";
  public from: Date | null = null;
  public until: Date | null = null;
  public isActive = false;

  constructor(source?: IPeriodModel) {
    if (source) {
      this.id = source.id;
      this.description = source.description;
      this.from = source.from ? new Date(source.from) : null;
      this.until = source.until ? new Date(source.until) : null;
      this.isActive = source.isActive;
    }
  }
}
