import { Component, Input } from "@angular/core";
import { AssignmentStatisticsModel } from "../../viewmodels/assignmentstatistics.model";

@Component({
  standalone: false,
  selector: "assignment-statistics",
  templateUrl: "./assignmentstatistics.component.html",
})
export class AssignmentStatisticsComponent {
  @Input()
  public model: AssignmentStatisticsModel;

  constructor() {
    this.model = new AssignmentStatisticsModel();
  }
}
