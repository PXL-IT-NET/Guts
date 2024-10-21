import { Component, Input } from "@angular/core";
import { ChartOptions } from "chart.js";
import { AssignmentSummaryModel } from "../../viewmodels/assignmentsummary.model";
import { ActivatedRoute, Router } from "@angular/router";

@Component({
  selector: "assignment-summary",
  templateUrl: "./assignmentsummary.component.html",
})
export class AssignmentSummaryComponent {
  @Input()
  public model: AssignmentSummaryModel;

  @Input()
  public topicType: string;

  @Input()
  public topicCode: string;

  @Input()
  public courseId: number;

  public chartOptions: ChartOptions<"doughnut"> = {};

  constructor(private router: Router, private route: ActivatedRoute) {
    this.model = new AssignmentSummaryModel();
    this.courseId = 0;
    this.topicType = '';
    this.topicCode = '';
  }

  public navigateToDetail(): void {
    if (this.topicType == "chapter") {
      this.router.navigate(["courses", this.courseId, "chapters", this.topicCode, "testresults"], {
        queryParams: { assignmentId: this.model.assignmentId },
      });
    }
    if (this.topicType == "project") {
      this.router.navigate(["courses", this.courseId, "projects", this.topicCode, "testresults"], {
        queryParams: { assignmentId: this.model.assignmentId },
      });
    }
  }
}
