import {
  ChangeDetectorRef,
  Component,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
} from "@angular/core";
import { AssignmentService } from "../../services/assignment.service";
import { ActivatedRoute } from "@angular/router";
import {
  IAssignmentDetailModel,
  AssignmentDetailModel,
} from "../../viewmodels/assignmentdetail.model";
import moment from "moment";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  selector: "app-assignment-detail",
  templateUrl: "./assignmentdetail.component.html",
  styleUrls: ["./assignmentdetail.component.css"],
})
export class AssignmentDetailComponent implements OnChanges, OnDestroy {
  private destroy$ = new Subject<void>();
  public model: AssignmentDetailModel;
  public inputsReady = false;
  public loading: boolean = false;

  @Input() public assignmentId: number;
  @Input() public teamId: number;
  @Input() public userId: number;
  @Input() public statusDate: moment.Moment;

  constructor(
    private route: ActivatedRoute,
    private assignmentService: AssignmentService,
    private cdr: ChangeDetectorRef,
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
    this.inputsReady = true;
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
      .pipe(takeUntil(this.destroy$))
      .subscribe((assignmentDetail: IAssignmentDetailModel) => {
        this.loading = false;
        this.model = new AssignmentDetailModel(assignmentDetail);
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
