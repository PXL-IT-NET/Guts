import { Component, EventEmitter, forwardRef, Input, OnInit, Output } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import * as moment from 'moment';

@Component({
  selector: 'app-ng-datetime',
  template: '<input type="datetime-local" class="form-control" [value]="_dateString" (change)="onDateChange($event.target.value)" />',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    multi: true,
    useExisting: forwardRef(() => NgDatetimeComponent)
  }]
})
export class NgDatetimeComponent implements OnInit, ControlValueAccessor {

  _dateString: string;
  onChange: (value: moment.Moment) => void;

  @Input()
  public set date(d: moment.Moment) {
    if(moment.isMoment(d)){
      this._dateString = this.parseDateToStringWithFormat(d);
    }
  }

  @Output() dateChange: EventEmitter<moment.Moment>;
  
  constructor() {
    this._dateString = '';
    this.dateChange = new EventEmitter();
    this.onChange = (value) => {};
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
      if (!Number.isNaN(parsedDate.getTime())) {
        this._dateString = value;
        let m = moment(parsedDate);
        this.dateChange.emit(m);
        this.onChange(m);
      }
    }
  }

  writeValue(obj: moment.Moment): void {
    this.date = obj;
  }
  registerOnChange(fn: (value: moment.Moment) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: any): void {
  }
  setDisabledState?(isDisabled: boolean): void {
  }
}
