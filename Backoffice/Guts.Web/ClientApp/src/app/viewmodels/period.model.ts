export interface IPeriodModel {
  id: number;
  description: string;
  from: Date | string | null;
  until: Date | string | null;
  isActive: boolean;
}

export class PeriodModel implements IPeriodModel {
  public id: number;
  public description: string;
  public from: Date | null;
  public until: Date | null;
  public isActive: boolean;

  constructor(source?: IPeriodModel) {
    this.id = 0;
    this.description = "";
    this.from = null;
    this.until = null;
    this.isActive = false;

    if (source) {
      this.id = source.id;
      this.description = source.description;
      this.from = source.from ? new Date(source.from) : null;
      this.until = source.until ? new Date(source.until) : null;
      this.isActive = source.isActive;
    }
  }
}
