import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ChapterService } from "../../services/chapter.service";
import { GetResult } from "../../util/result";
import { IChapterDetailsModel } from "../../viewmodels/chapter.model";
import { IUserModel } from "../../viewmodels/user.model";
import { Subject } from "rxjs";
import moment from "moment";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: "./chapter.component.html",
})
export class ChapterComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  public model: IChapterDetailsModel;
  public selectedAssignmentId: number;
  public selectedUser: IUserModel;
  public selectedDate: moment.Moment;
  public datePickerSettings: any;
  public loading: boolean = false;
  public courseId: number;
  public chapterCode: string;

  constructor(
    private chapterService: ChapterService,
    private route: ActivatedRoute,
    private toastr: ToastrService,
  ) {
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
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      var parentParams = this.route.parent.snapshot.params;
      this.courseId = +parentParams["courseId"]; // (+) converts 'courseId' to a number
      this.chapterCode = params["chapterCode"];
      this.loading = true;
      this.chapterService
        .getChapterDetails(this.courseId, this.chapterCode)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result: GetResult<IChapterDetailsModel>) => {
          this.loading = false;
          if (result.success) {
            this.model = result.value;
            this.selectedUser = result.value.users[0];
          } else {
            this.toastr.error(
              "Could not load chapter details from API. Message: " +
                (result.message || "unknown error"),
              "System error",
            );
          }
        });
    });

    // Subscribe to queryParams to detect changes in query string parameters
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((queryParams) => {
        if (queryParams["assignmentId"]) {
          this.selectedAssignmentId = +queryParams["assignmentId"];
        } else {
          this.selectedAssignmentId = 0;
        }
      });
  }

  ngOnDestroy(): void {}

  public formatUser = (user: IUserModel): string => {
    return user ? user.fullName : "";
  };
}
