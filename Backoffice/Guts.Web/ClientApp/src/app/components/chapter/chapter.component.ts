import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ChapterService } from "../../services/chapter.service";
import { GetResult } from "../../util/result";
import { IChapterDetailsModel } from "../../viewmodels/chapter.model";
import { IUserModel } from "../../viewmodels/user.model";
import moment from "moment";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./chapter.component.html",
})
export class ChapterComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);

  private _model = signal<IChapterDetailsModel>({
    id: 0,
    code: "",
    description: "",
    exercises: [],
    users: [],
    assignments: [],
  });
  public get model(): IChapterDetailsModel {
    return this._model();
  }
  public set model(v: IChapterDetailsModel) {
    this._model.set(v);
  }

  private _selectedAssignmentId = signal<number>(0);
  public get selectedAssignmentId(): number {
    return this._selectedAssignmentId();
  }
  public set selectedAssignmentId(v: number) {
    this._selectedAssignmentId.set(v);
  }

  private _selectedUser = signal<IUserModel | null>(null);
  public get selectedUser(): IUserModel | null {
    return this._selectedUser();
  }
  public set selectedUser(v: IUserModel | null) {
    this._selectedUser.set(v);
  }

  private _selectedDate = signal<moment.Moment>(moment());
  public get selectedDate(): moment.Moment {
    return this._selectedDate();
  }
  public set selectedDate(v: moment.Moment) {
    this._selectedDate.set(v);
  }

  public datePickerSettings: any;

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public courseId: number;
  public chapterCode: string;

  constructor(
    private chapterService: ChapterService,
    private route: ActivatedRoute,
    private toastr: ToastrService,
  ) {
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
      const parentParams = this.route.parent.snapshot.params;
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
