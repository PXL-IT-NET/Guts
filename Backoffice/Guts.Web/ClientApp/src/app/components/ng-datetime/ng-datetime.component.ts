import {
  Component,
  EventEmitter,
  forwardRef,
  Input,
  Output,
  ChangeDetectionStrategy,
  signal,
} from "@angular/core";
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";
import { formatDateTimeLocal } from "../../util/date.util";

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

  onChange: (value: Date) => void;

  @Input()
  public set date(d: Date | string | null) {
    if (d) {
      const dt = typeof d === "string" ? new Date(d) : d;
      if (!Number.isNaN(dt.getTime())) {
        this._dateString = formatDateTimeLocal(dt);
      }
    } else {
      this._dateString = "";
    }
  }

  @Output() dateChange: EventEmitter<Date>;

  constructor() {
    this.dateChange = new EventEmitter();
    this.onChange = () => {};
  }

  onDateChange(value: string): void {
    if (value !== this._dateString) {
      const parsedDate = value ? new Date(value) : new Date();
      if (!Number.isNaN(parsedDate.getTime())) {
        this._dateString = value;
        this.dateChange.emit(parsedDate);
        this.onChange(parsedDate);
      }
    }
  }

  writeValue(obj: Date | string | null): void {
    this.date = obj;
  }
  registerOnChange(fn: (value: Date) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: any): void {
    // ControlValueAccessor touch registration callback placeholder
  }
  setDisabledState?(isDisabled: boolean): void {
    // ControlValueAccessor disabled state placeholder
  }
}
