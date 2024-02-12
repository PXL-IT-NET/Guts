import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-assessment-score-dropdown',
  templateUrl: './assessment-score-dropdown.component.html'
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
