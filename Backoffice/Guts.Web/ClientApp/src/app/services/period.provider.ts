import { BehaviorSubject } from "rxjs";
import { IPeriodModel } from "../viewmodels/period.model";
import { Injectable } from "@angular/core";

@Injectable()
export class PeriodProvider{
    public period$: BehaviorSubject<IPeriodModel> = new BehaviorSubject<IPeriodModel>(null);

    public getPeriod(): IPeriodModel {
        return this.period$.getValue();
    }

    public setPeriod(period: IPeriodModel): void {
        this.period$.next(period);
    }
}