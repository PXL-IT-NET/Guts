import { Injectable, signal } from "@angular/core";
import { BehaviorSubject } from "rxjs";
import { IPeriodModel } from "../viewmodels/period.model";

@Injectable({
  providedIn: "root",
})
export class PeriodProvider {
  private readonly _period = signal<IPeriodModel | null>(null);
  public readonly period$: BehaviorSubject<IPeriodModel | null> =
    new BehaviorSubject<IPeriodModel | null>(null);

  /**
   * Reactive signal for the current period
   */
  public readonly selectedPeriod = this._period.asReadonly();

  get period(): IPeriodModel | null {
    return this._period();
  }

  set period(p: IPeriodModel | null) {
    this._period.set(p);
    this.period$.next(p);
  }
}

