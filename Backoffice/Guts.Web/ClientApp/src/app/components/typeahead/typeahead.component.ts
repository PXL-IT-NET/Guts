import {
  Component,
  forwardRef,
  Input,
  OnChanges,
  OnDestroy,
  output,
  SimpleChanges,
  ViewChild,
  ChangeDetectionStrategy,
  signal,
} from "@angular/core";
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";
import {
  NgbTypeahead,
  NgbTypeaheadSelectItemEvent,
} from "@ng-bootstrap/ng-bootstrap";
import { Observable, Subject, mergeWith } from "rxjs";
import {
  debounceTime,
  distinctUntilChanged,
  filter,
  map,
} from "rxjs/operators";

@Component({
  standalone: false,
  selector: "app-typeahead",
  templateUrl: "./typeahead.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TypeaheadComponent),
      multi: true,
    },
  ],
})
export class TypeaheadComponent<T = any>
  implements ControlValueAccessor, OnChanges, OnDestroy
{
  @Input() public items: T[] = [];

  @Input()
  public set data(value: T[]) {
    this.items = value || [];
  }
  public get data(): T[] {
    return this.items;
  }

  private readonly _selectedItem = signal<T | null>(null);
  @Input() public get selectedItem(): T | null {
    return this._selectedItem();
  }
  public set selectedItem(v: T | null) {
    this._selectedItem.set(v);
  }
  public readonly selectedItemChange = output<T | null>();

  @Input() public displayProperty: string = "";
  @Input() public formatItem?: (item: T) => string;
  @Input() public searchPredicate?: (item: T, term: string) => boolean;

  @Input() public id: string = "";
  @Input() public name: string = "";
  @Input() public placeholder: string = "";
  @Input() public readonly: boolean = false;

  private readonly _disabled = signal<boolean>(false);
  @Input() public get disabled(): boolean {
    return this._disabled();
  }
  public set disabled(v: boolean) {
    this._disabled.set(v);
  }

  @Input() public inputClass: string = "form-control";
  @Input() public clearOnClick: boolean = true;
  @Input() public debounceTime: number = 200;
  @Input() public allowCustomValue: boolean = false;

  public readonly itemSelected = output<T>();

  @ViewChild("instance", { static: false })
  public typeaheadInstance!: NgbTypeahead;

  private readonly _innerValue = signal<any>(null);
  public get innerValue(): any {
    return this._innerValue();
  }
  public set innerValue(v: any) {
    this._innerValue.set(v);
  }
  public click$ = new Subject<string>();

  public onChange: (value: T | null) => void = () => {};
  public onTouched: () => void = () => {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.selectedItem) {
      this.innerValue = this.selectedItem;
    }
  }

  ngOnDestroy(): void {
    this.click$.complete();
  }

  public formatter = (item: T | null | undefined): string => {
    if (item == null) {
      return "";
    }
    if (this.formatItem) {
      return this.formatItem(item);
    }
    if (this.displayProperty && typeof item === "object") {
      const val = (item as any)[this.displayProperty];
      return val != null ? String(val) : "";
    }
    if (typeof item === "string") {
      return item;
    }
    if (typeof (item as any).toString === "function") {
      const str = (item as any).toString();
      if (str !== "[object Object]") {
        return str;
      }
    }
    return "";
  };

  public search = (text$: Observable<string>): Observable<T[]> => {
    const debouncedText$ = text$.pipe(
      debounceTime(this.debounceTime),
      distinctUntilChanged(),
    );
    const clicksWithClosedPopup$ = this.click$.pipe(
      filter(
        () => !this.typeaheadInstance?.isPopupOpen(),
      ),
    );

    return clicksWithClosedPopup$.pipe(
      mergeWith(debouncedText$),
      map((term: string) => {
        const items = this.items || [];
        if (!term || term.length === 0) {
          return items;
        }
        const normalizedTerm = term.toLowerCase();
        if (this.searchPredicate) {
          return items.filter((item) =>
            this.searchPredicate!(item, normalizedTerm),
          );
        }
        return items.filter((item) => {
          const text = this.formatter(item);
          return text.toLowerCase().includes(normalizedTerm);
        });
      }),
    );
  };

  public onClick(): void {
    if (this.readonly || this.disabled) {
      return;
    }
    if (this.clearOnClick) {
      this.updateValue(null, true);
    }
    this.click$.next("");
  }

  public onBlur(): void {
    this.onTouched();
  }

  public onSelectItem(event: NgbTypeaheadSelectItemEvent<T>): void {
    this.updateValue(event.item, true);
    this.itemSelected.emit(event.item);
  }

  public onModelChange(val: any): void {
    if (val === "" || val == null) {
      this.updateValue(null, true);
    } else if (typeof val === "object") {
      this.updateValue(val, true);
    } else if (
      this.items?.some((item) => typeof item === "string" && item === val)
    ) {
      this.updateValue(val, true);
    } else if (this.allowCustomValue) {
      this.updateValue(val, true);
    } else {
      this.innerValue = val;
      if (this.selectedItem !== null) {
        this.selectedItem = null;
        this.onChange(null);
        this.selectedItemChange.emit(null);
      }
    }
  }

  private updateValue(val: T | null, emitEvents: boolean = true): void {
    const changed = this.selectedItem !== val;
    this.innerValue = val;
    this.selectedItem = val;
    if (emitEvents && changed) {
      this.onChange(val);
      this.selectedItemChange.emit(val);
    }
  }

  public writeValue(value: any): void {
    this.innerValue = value;
    this.selectedItem = value;
  }

  public registerOnChange(fn: (value: T | null) => void): void {
    this.onChange = fn;
  }

  public registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  public setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}

