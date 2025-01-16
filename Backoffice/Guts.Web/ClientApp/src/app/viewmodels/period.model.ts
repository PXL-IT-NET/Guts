import * as moment from "moment";

export interface IPeriodModel {
    id: number;
    description: string;
    from: moment.Moment;
    until: moment.Moment;
    isActive: boolean;
}

export class PeriodModel implements IPeriodModel {
    public id: number;
    public description: string;
    public from: moment.Moment;
    public until: moment.Moment;
    public isActive: boolean;

    constructor(source?: IPeriodModel) {
        this.id = 0;
        this.description = '';
        this.from = null;
        this.until = null;
        this.isActive = false;

        if (source) {
            this.id = source.id;
            this.description = source.description;
            this.from = moment(source.from);
            this.until = moment(source.until);
            this.isActive = source.isActive;
        }
    }
}