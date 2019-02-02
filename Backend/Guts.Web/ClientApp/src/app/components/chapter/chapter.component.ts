import { Component, OnInit, OnDestroy } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { TopicContextProvider, TopicContext} from "../../services/topic.context.provider";
import { ActivatedRoute, Router } from '@angular/router';
import { IChapterDetailsModel } from '../../viewmodels/chapter.model';
import { GetResult } from "../../util/Result";
import { ToastrService } from 'ngx-toastr';
import * as moment from 'moment';

@Component({
  templateUrl: './chapter.component.html'
})
export class ChapterComponent implements OnInit, OnDestroy {

  public model: IChapterDetailsModel;
  public selectedAssignmentId: number;
  public selectedUserId: number;
  public selectedDate: Date;
 // public context: TopicContext;
  public datePickerSettings: any;
  public loading: boolean = false;

  private courseId: number;
  private chapterCode: string;

  constructor(private chapterService: ChapterService,
    private topicContextProvider: TopicContextProvider,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService) {

    this.model = {
      id: 0,
      code: '',
      description: '',
      exercises: [],
      users: []
    };
    this.selectedAssignmentId = 0;
    this.selectedUserId = 0;
    this.selectedDate = new Date();
    this.datePickerSettings = {
      bigBanner: true,
      timePicker: true,
      format: 'dd-MM-yyyy HH:mm'
    };
    this.courseId = 0;
    this.chapterCode = '';
  }

  ngOnInit() {
    this.route.params.subscribe(params => {

      var parentParams = this.route.parent.snapshot.params;
      this.courseId = +parentParams['courseId']; // (+) converts 'courseId' to a number
      this.chapterCode = params['chapterCode'];

      this.loading = true;
      this.chapterService.getChapterDetails(this.courseId, this.chapterCode).subscribe((result: GetResult<IChapterDetailsModel>) => {
        this.loading = false;
        if (result.success) {
          this.model = result.value;
          this.selectedAssignmentId = 0;
          this.selectedUserId = result.value.users[0].id;
          this.navigateToSummaryForSelectedUser();
          this.loadStatistics();
        } else {
          this.toastr.error("Could not load chapter details from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
    });
  }

  ngOnDestroy() {

  }

  public onSelectionChanged() {
    if (this.selectedAssignmentId > 0) {
      this.router.navigate(['users', this.selectedUserId, 'exercises', this.selectedAssignmentId], { relativeTo: this.route });
    } else {
      this.navigateToSummaryForSelectedUser();
    }
  }

  private navigateToSummaryForSelectedUser() {
    this.topicContextProvider.setTopic(this.courseId, this.model, moment(this.selectedDate));
    this.router.navigate(['users', this.selectedUserId, 'summary'], { relativeTo: this.route });
  }

  public onDateChanged() {
    this.topicContextProvider.setTopic(this.courseId, this.model, moment(this.selectedDate));
    this.loadStatistics();
  }

  private loadStatistics() {
    this.chapterService.getChapterStatistics(this.courseId, this.chapterCode, moment(this.selectedDate))
      .subscribe((result) => {
        if (result.success) {
          this.topicContextProvider.setStatistics(result.value);
        } else {
          this.toastr.error("Could not load chapter statistics from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
  }
}
