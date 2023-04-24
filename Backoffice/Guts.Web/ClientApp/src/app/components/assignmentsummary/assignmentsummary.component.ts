import { Component, Input } from '@angular/core';
import { ChartOptions } from 'chart.js';
import { AssignmentSummaryModel } from '../../viewmodels/assignmentsummary.model';

@Component({
  selector: 'assignment-summary',
  templateUrl: './assignmentsummary.component.html'
})
export class AssignmentSummaryComponent{
  @Input()
  public model: AssignmentSummaryModel;

  public chartOptions: ChartOptions<'doughnut'> = {
  };

  constructor() {
    this.model = new AssignmentSummaryModel();
  }
}
