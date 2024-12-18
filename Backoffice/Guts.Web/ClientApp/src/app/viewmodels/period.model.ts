import * as moment from "moment";

export interface IPeriodModel {
    id: number;
    description: string;
    from: moment.Moment;
    until: moment.Moment;
}

export class PeriodModel implements IPeriodModel {
    public id: number;
    public description: string;
    public from: moment.Moment;
    public until: moment.Moment;

    constructor(source?: IPeriodModel) {
        this.id = 0;
        this.description = '';
        this.from = null;
        this.until = null;

        if (source) {
            this.id = source.id;
            this.description = source.description;
            this.from = moment(source.from);
            this.until = moment(source.until);
        }
    }
}