import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { AssignmentService } from '../../services/assignment.service';
import { ActivatedRoute } from '@angular/router';
import { IAssignmentDetailModel, AssignmentDetailModel } from "../../viewmodels/assignmentdetail.model";
import * as moment from "moment";

@Component({
  selector: 'app-assignment-detail',
  templateUrl: './assignmentdetail.component.html',
  styleUrls: ['./assignmentdetail.component.css']
})
export class AssignmentDetailComponent implements OnChanges {
  public model: AssignmentDetailModel;
  public loading: boolean = false;

  @Input() public assignmentId: number;
  @Input() public teamId: number;
  @Input() public userId: number;
  @Input() public statusDate: moment.Moment;

  constructor(private route: ActivatedRoute,
    private assignmentService: AssignmentService) {

    this.model = new AssignmentDetailModel();
    this.assignmentId = 0;
    this.userId = 0;
    this.teamId = 0;
    this.statusDate = moment();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.assignmentId > 0  && (this.userId > 0 || this.teamId > 0) ) {
      this.loadAssignment();
    }
  }

  private loadAssignment() {
    this.loading = true;
    this.assignmentService.getAssignmentDetail(this.assignmentId, this.userId, this.teamId, this.statusDate).subscribe((assignmentDetail: IAssignmentDetailModel) => {
      this.loading = false;
      this.model = new AssignmentDetailModel(assignmentDetail);
    });
  }
}
