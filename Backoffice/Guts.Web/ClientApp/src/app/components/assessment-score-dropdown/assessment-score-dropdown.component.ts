import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-assessment-score-dropdown",
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: "./assessment-score-dropdown.component.html",
})
export class AssessmentScoreDropdownComponent {
  @Input() public value: number;
  @Input() public disabled: boolean;
  @Output() valueChange = new EventEmitter<number>();

  public constructor() {
    this.disabled = false;
  }

  onValueChange(newValue: number) {
    this.value = newValue;
    this.valueChange.emit(newValue);
  }
}
