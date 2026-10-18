import { Component, Input, ChangeDetectionStrategy } from "@angular/core";
import { AssignmentStatisticsModel } from "../../viewmodels/assignmentstatistics.model";

@Component({
  standalone: false,
  selector: "assignment-statistics",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./assignmentstatistics.component.html",
})
export class AssignmentStatisticsComponent {
  @Input()
  public model: AssignmentStatisticsModel;

  constructor() {
    this.model = new AssignmentStatisticsModel();
  }
}
