import { Component, OnDestroy, OnInit, ViewChild } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ChapterService } from "../../services/chapter.service";
import { GetResult } from "../../util/result";
import { IChapterDetailsModel } from "../../viewmodels/chapter.model";
import { IUserModel } from "../../viewmodels/user.model";
import { Observable, Subject } from "rxjs";
import {
  debounceTime,
  map,
  distinctUntilChanged,
  filter,
  merge,
} from "rxjs/operators";
import { NgbTypeahead } from "@ng-bootstrap/ng-bootstrap";
import * as moment from "moment";

@Component({
  templateUrl: "./chapter.component.html",
})
export class ChapterComponent implements OnInit, OnDestroy {
  public model: IChapterDetailsModel;
  public selectedAssignmentId: number;
  public selectedUser: IUserModel;
  public selectedDate: moment.Moment;
  public datePickerSettings: any;
  public loading: boolean = false;
  public courseId: number;
  public chapterCode: string;

  private userClick$: Subject<string>;
  @ViewChild("instance", { static: false }) userTypeAheadInstance: NgbTypeahead;

  constructor(
    private chapterService: ChapterService,
    private route: ActivatedRoute,
    private toastr: ToastrService
  ) {
    this.userClick$ = new Subject<string>();

    this.model = {
      id: 0,
      code: "",
      description: "",
      exercises: [],
      users: [],
      assignments: [],
    };
    this.selectedAssignmentId = 0;
    this.selectedUser = null;
    this.selectedDate = moment();
    this.datePickerSettings = {
      bigBanner: true,
      timePicker: true,
      format: "dd-MM-yyyy HH:mm",
    };
    this.courseId = 0;
    this.chapterCode = "";
  }

  ngOnInit() {
    this.route.params.subscribe((params) => {
      var parentParams = this.route.parent.snapshot.params;
      this.courseId = +parentParams["courseId"]; // (+) converts 'courseId' to a number
      this.chapterCode = params["chapterCode"];
      this.loading = true;
      this.chapterService
        .getChapterDetails(this.courseId, this.chapterCode)
        .subscribe((result: GetResult<IChapterDetailsModel>) => {
          this.loading = false;
          if (result.success) {
            this.model = result.value;
            this.selectedAssignmentId = 0;
            this.selectedUser = result.value.users[0];
          } else {
            this.toastr.error(
              "Could not load chapter details from API. Message: " +
                (result.message || "unknown error"),
              "API error"
            );
          }
        });
    });

    // Subscribe to queryParams to detect changes in query string parameters
    this.route.queryParams.subscribe((queryParams) => {
      if (queryParams["assignmentId"]) {
        this.selectedAssignmentId = +queryParams["assignmentId"];
      } else{
        this.selectedAssignmentId = 0;
      }
    });
  }

  ngOnDestroy() {}

  public onUserClick() {
    this.selectedUser = null;
    this.userClick$.next("");
  }

  public searchUsers = (text$: Observable<string>) => {
    const debouncedText$: Observable<string> = text$.pipe(
      debounceTime(200),
      distinctUntilChanged()
    );
    const clicksWithClosedPopup$: Observable<string> = this.userClick$.pipe(
      filter(() => !this.userTypeAheadInstance.isPopupOpen())
    );

    return clicksWithClosedPopup$.pipe(
      merge(debouncedText$),
      map((term: string) => {
        if (!term || term.length === 0) {
          return this.model.users;
        } else {
          term = term.toLowerCase();
          return this.model.users.filter(
            (u) => u.fullName.toLowerCase().indexOf(term) > -1
          );
        }
      })
    );
  };

  public formatUser = (user: IUserModel) => {
    return user.fullName;
  };
}
