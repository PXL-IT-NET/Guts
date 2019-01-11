import { Component } from '@angular/core';
import { AssignmentService } from '../../services/assignment.service';
import { ActivatedRoute } from '@angular/router';
import { IAssignmentDetailModel, AssignmentDetailModel } from "../../viewmodels/assignmentdetail.model";
import { ChapterContextProvider } from '../../services/chapter.context.provider';

@Component({
  templateUrl: './assignmentdetail.component.html',
  styleUrls: ['./assignmentdetail.component.css']
})
export class AssignmentDetailComponent {
  public model: AssignmentDetailModel;
  public loading: boolean = false;

  private assignmentId: number;
  private userId: number;

  constructor(private route: ActivatedRoute,
    private assignmentService: AssignmentService,
    private chapterContextProvider: ChapterContextProvider) {

    this.model = new AssignmentDetailModel();
    this.assignmentId = 0;
    this.userId = 0;

    this.chapterContextProvider.contextChanged$.subscribe(() => {
        this.loadAssignment();
    });
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.assignmentId = +params['assignmentId']; // (+) converts 'exerciseId' to a number
      if (params['userId']) this.userId = +params['userId'];
      this.loadAssignment();
    });
  }

  private loadAssignment() {
    this.loading = true;
    this.assignmentService.getAssignmentDetail(this.assignmentId, this.userId, this.chapterContextProvider.context.statusDate).subscribe((assignmentDetail: IAssignmentDetailModel) => {
      this.loading = false;
      this.model = new AssignmentDetailModel(assignmentDetail);
    });
  }
}
