import { BehaviorSubject } from "rxjs";
import { IPeriodModel } from "../viewmodels/period.model";
import { Injectable } from "@angular/core";

@Injectable()
export class PeriodProvider{
    public period$: BehaviorSubject<IPeriodModel> = new BehaviorSubject<IPeriodModel>(null);

    //transform into getter

    get period(): IPeriodModel {
        return this.period$.getValue();
    }

    set period(p: IPeriodModel) {
        this.period$.next(p);
    }
}