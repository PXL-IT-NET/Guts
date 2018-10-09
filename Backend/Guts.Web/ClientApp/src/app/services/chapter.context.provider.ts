import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
 

export class ChapterContext {

  private _statusDate: Date;
  public get statusDate(): Date {
    return this._statusDate;
  }
  public set statusDate(date: any) {
    this._statusDate = new Date(date);
  }

  constructor() {
    this._statusDate = new Date();
  }
}

@Injectable()
export class ChapterContextProvider {
  private contextSource: Subject<void>;

  public contextChanged$: Observable<void>;
  public context : ChapterContext;


  constructor() {
    this.contextSource = new Subject<void>();
    this.contextChanged$ = this.contextSource.asObservable();
    this.context = new ChapterContext();
  }

  public announceContextChange() {
    this.contextSource.next();
  }
}
