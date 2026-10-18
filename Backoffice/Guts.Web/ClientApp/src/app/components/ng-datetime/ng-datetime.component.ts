import {
  Component,
  EventEmitter,
  forwardRef,
  Input,
  Output,
  ChangeDetectionStrategy,
  signal
} from "@angular/core";
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";
import moment from "moment";

@Component({
  standalone: false,
  selector: "app-ng-datetime",
  template:
    '<input type="datetime-local" class="form-control" [value]="_dateString" (change)="onDateChange($event.target.value)" />',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      multi: true,
      useExisting: forwardRef(() => NgDatetimeComponent),
    },
  ],
})
export class NgDatetimeComponent implements ControlValueAccessor {
  private _dateStringSignal = signal<string>("");

  public get _dateString(): string {
    return this._dateStringSignal();
  }
  public set _dateString(value: string) {
    this._dateStringSignal.set(value);
  }

  onChange: (value: moment.Moment) => void;

  @Input()
  public set date(d: moment.Moment) {
    if (moment.isMoment(d)) {
      this._dateString = this.parseDateToStringWithFormat(d);
    }
  }

  @Output() dateChange: EventEmitter<moment.Moment>;

  constructor() {
    this.dateChange = new EventEmitter();
    this.onChange = (value) => {};
  }

  private parseDateToStringWithFormat(date: moment.Moment): string {
    return date.format("YYYY-MM-DDTHH:mm");
  }

  onDateChange(value: string): void {
    if (value !== this._dateString) {
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
  registerOnTouched(fn: any): void {}
  setDisabledState?(isDisabled: boolean): void {}
}
