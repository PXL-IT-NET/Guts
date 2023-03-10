import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import * as moment from 'moment';

@Component({
  selector: 'app-ng-datetime',
  template: '<input type="datetime-local" class="form-control" [value]="_dateString" (change)="onDateChange($event.target.value)" />',
})
export class NgDatetimeComponent implements OnInit {

  _dateString: string;

  @Input()
  public set date(d: moment.Moment) {
    if(moment.isMoment(d)){
      this._dateString = this.parseDateToStringWithFormat(d);
    }
  }

  @Output() dateChange: EventEmitter<moment.Moment>;
  
  constructor() {
    this._dateString = this.parseDateToStringWithFormat(moment());
    this.dateChange = new EventEmitter();
  }

  ngOnInit(): void {
  }

  private parseDateToStringWithFormat(date: moment.Moment): string {
    let result: string;
    result = [date.year(), '-', date.month(), '-', date.day(), 'T', date.hour(), ':', date.minute()].join('');
    let dd = date.date().toString();
    let mm = (date.month() + 1).toString();
    let hh = date.hour().toString();
    let min = date.minute().toString();
    dd = dd.length === 2 ? dd : "0" + dd;
    mm = mm.length === 2 ? mm : "0" + mm;
    hh = hh.length === 2 ? hh : "0" + hh;
    min = min.length === 2 ? min : "0" + min;
    result = [date.year(), '-', mm, '-', dd, 'T', hh, ':', min].join('');
    return result;
  }

  onDateChange(value: string): void {
    if (value != this._dateString) {

      let parsedDate = value ? new Date(value) : new Date();
      // check if date is valid first
      if (parsedDate.getTime() != NaN) {
        this._dateString = value;
        this.dateChange.emit(moment(parsedDate));
      }
    }
  }
}