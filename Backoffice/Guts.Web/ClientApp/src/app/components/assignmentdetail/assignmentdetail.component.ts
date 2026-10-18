import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, Input, OnChanges, OnDestroy, SimpleChanges, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { AssignmentService } from "../../services/assignment.service";
import { ActivatedRoute } from "@angular/router";
import {
  IAssignmentDetailModel,
  AssignmentDetailModel,
} from "../../viewmodels/assignmentdetail.model";
import moment from "moment";
import { Subject } from "rxjs";

@Component({
  standalone: false,
  selector: "app-assignment-detail",
  templateUrl: "./assignmentdetail.component.html",
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ["./assignmentdetail.component.css"],
})
export class AssignmentDetailComponent implements OnChanges, OnDestroy {
  private destroyRef = inject(DestroyRef);
  public model: AssignmentDetailModel;
  public loading: boolean = true;

  @Input() public assignmentId: number;
  @Input() public teamId: number;
  @Input() public userId: number;
  @Input() public statusDate: moment.Moment;

  constructor(
    private route: ActivatedRoute,
    private assignmentService: AssignmentService,
  ) {
    this.model = new AssignmentDetailModel();
    this.assignmentId = 0;
    this.userId = 0;
    this.teamId = 0;
    this.statusDate = moment();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.assignmentId > 0 && (this.userId > 0 || this.teamId > 0)) {
      this.loadAssignment();
    }
  }

  private loadAssignment() {
    this.loading = true;
    this.assignmentService
      .getAssignmentDetail(
        this.assignmentId,
        this.userId,
        this.teamId,
        this.statusDate,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((assignmentDetail: IAssignmentDetailModel) => {
        this.loading = false;
        this.model = new AssignmentDetailModel(assignmentDetail);
      });
  }

  ngOnDestroy(): void {}
}
