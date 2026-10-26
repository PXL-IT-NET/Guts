import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, Input, OnChanges, SimpleChanges, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { AssignmentService } from "../../services/assignment.service";
import { ActivatedRoute } from "@angular/router";
import {
  IAssignmentDetailModel,
  AssignmentDetailModel,
} from "../../viewmodels/assignmentdetail.model";

@Component({
  standalone: false,
  selector: "app-assignment-detail",
  templateUrl: "./assignmentdetail.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ["./assignmentdetail.component.css"],
})
export class AssignmentDetailComponent implements OnChanges {
  private readonly destroyRef = inject(DestroyRef);

  private readonly _model = signal<AssignmentDetailModel>(new AssignmentDetailModel());
  public get model(): AssignmentDetailModel {
    return this._model();
  }
  public set model(v: AssignmentDetailModel) {
    this._model.set(v);
  }

  private readonly _loading = signal<boolean>(true);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  @Input() public assignmentId = 0;
  @Input() public teamId = 0;
  @Input() public userId = 0;
  @Input() public statusDate: Date = new Date();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly assignmentService: AssignmentService,
  ) {}

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


}
