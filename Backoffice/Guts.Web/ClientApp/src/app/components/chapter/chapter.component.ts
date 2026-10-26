import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ToastrService } from "ngx-toastr";
import { ChapterService } from "../../services/chapter.service";
import { GetResult } from "../../util/result";
import { IChapterDetailsModel } from "../../viewmodels/chapter.model";
import { IUserModel } from "../../viewmodels/user.model";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./chapter.component.html",
})
export class ChapterComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  private readonly _model = signal<IChapterDetailsModel>({
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

  private readonly _selectedAssignmentId = signal<number>(0);
  public get selectedAssignmentId(): number {
    return this._selectedAssignmentId();
  }
  public set selectedAssignmentId(v: number) {
    this._selectedAssignmentId.set(v);
  }

  private readonly _selectedUser = signal<IUserModel | null>(null);
  public get selectedUser(): IUserModel | null {
    return this._selectedUser();
  }
  public set selectedUser(v: IUserModel | null) {
    this._selectedUser.set(v);
  }

  private readonly _selectedDate = signal<Date>(new Date());
  public get selectedDate(): Date {
    return this._selectedDate();
  }
  public set selectedDate(v: Date) {
    this._selectedDate.set(v);
  }

  public datePickerSettings: any;

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  public courseId: number;
  public chapterCode: string;

  constructor(
    private readonly chapterService: ChapterService,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService,
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



  public formatUser = (user: IUserModel): string => {
    return user ? user.fullName : "";
  };
}
