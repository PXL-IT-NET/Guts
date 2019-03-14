import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import * as moment from 'moment';
import { ToastrService } from 'ngx-toastr';
import { ChapterService } from '../../services/chapter.service';
import { TopicContextProvider } from "../../services/topic.context.provider";
import { GetResult } from "../../util/Result";
import { IChapterDetailsModel } from '../../viewmodels/chapter.model';
import { IUserModel } from '../../viewmodels/user.model';
import { Observable, Subject } from 'rxjs';
import { debounceTime, map, distinctUntilChanged, filter, merge } from 'rxjs/operators';
import { NgbTypeahead } from '@ng-bootstrap/ng-bootstrap';

@Component({
  templateUrl: './chapter.component.html'
})
export class ChapterComponent implements OnInit, OnDestroy {

  public model: IChapterDetailsModel;
  public selectedAssignmentId: number;
  public selectedUser: IUserModel;
  public selectedDate: Date;
  public datePickerSettings: any;
  public loading: boolean = false;
  

  private courseId: number;
  private chapterCode: string;

  private userClick$: Subject<string>;
  @ViewChild('instance') userTypeAheadInstance: NgbTypeahead;

  constructor(private chapterService: ChapterService,
    private topicContextProvider: TopicContextProvider,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService) {

    this.userClick$ = new Subject<string>();

    this.model = {
      id: 0,
      code: '',
      description: '',
      exercises: [],
      users: []
    };
    this.selectedAssignmentId = 0;
    this.selectedUser = null;
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
          this.selectedUser = result.value.users[0];
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

  public onUserClick() {
    this.selectedUser = null;
    this.userClick$.next('');
  }

  public onSelectionChanged() {
    if (!this.selectedUser.id) return; //No user selected. Client may be typing

    if (this.selectedAssignmentId > 0) {
      this.router.navigate(['users', this.selectedUser.id, 'exercises', this.selectedAssignmentId], { relativeTo: this.route });
    } else {
      this.navigateToSummaryForSelectedUser().then(() => {
        this.topicContextProvider.resendStatistics();
      });
    }
  }

  private navigateToSummaryForSelectedUser() : Promise<boolean> {
    this.topicContextProvider.setTopic(this.courseId, this.model, moment(this.selectedDate));
    return this.router.navigate(['users', this.selectedUser.id, 'summary'], { relativeTo: this.route });
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

  public searchUsers = (text$: Observable<string>) => {

    const debouncedText$: Observable<string> = text$.pipe(debounceTime(200), distinctUntilChanged());
    const clicksWithClosedPopup$: Observable<string> = this.userClick$.pipe(filter(() => !this.userTypeAheadInstance.isPopupOpen()));

    return clicksWithClosedPopup$.merge(debouncedText$).pipe(
      map((term: string) => {
        if (!term || term.length === 0) {
          return this.model.users;
        } else {
          term = term.toLowerCase();
          return this.model.users.filter(u => u.fullName.toLowerCase().indexOf(term) > -1);
        }
      })
    );
  };

  public formatUser = (user: IUserModel) => {
    return user.fullName;
  };
}
