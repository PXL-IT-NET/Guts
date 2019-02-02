import { Component, Input } from '@angular/core';
import { AssignmentSummaryModel } from '../../viewmodels/assignmentsummary.model';

@Component({
  selector: 'assignment-summary',
  templateUrl: './assignmentsummary.component.html'
})
export class AssignmentSummaryComponent{
  @Input()
  public model: AssignmentSummaryModel;

  constructor() {
    this.model = new AssignmentSummaryModel();
  }
}
