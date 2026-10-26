import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-assessment-score-dropdown",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./assessment-score-dropdown.component.html",
})
export class AssessmentScoreDropdownComponent {
  @Input() public value: number;
  @Input() public disabled = false;
  @Output() valueChange = new EventEmitter<number>();

  onValueChange(newValue: number) {
    this.value = newValue;
    this.valueChange.emit(newValue);
  }
}
