import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { IProjectDetailsModel } from "../../viewmodels/project.model";
import { ProjectService } from "../../services/project.service";
import { GetResult } from "../../util/result";
import { ToastrService } from "ngx-toastr";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./project.component.html",
})
export class ProjectComponent implements OnInit {
  private destroyRef = inject(DestroyRef);

  private _model = signal<IProjectDetailsModel>({
    id: 0,
    code: "",
    description: "",
    components: [],
    teams: [],
    assignments: [],
  });
  public get model(): IProjectDetailsModel {
    return this._model();
  }
  public set model(v: IProjectDetailsModel) {
    this._model.set(v);
  }

  private _selectedDate = signal<Date>(new Date());
  public get selectedDate(): Date {
    return this._selectedDate();
  }
  public set selectedDate(v: Date) {
    this._selectedDate.set(v);
  }

  private _selectedAssignmentId = signal<number>(0);
  public get selectedAssignmentId(): number {
    return this._selectedAssignmentId();
  }
  public set selectedAssignmentId(v: number) {
    this._selectedAssignmentId.set(v);
  }

  private _selectedTeamId = signal<number>(0);
  public get selectedTeamId(): number {
    return this._selectedTeamId();
  }
  public set selectedTeamId(v: number) {
    this._selectedTeamId.set(v);
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
  public projectCode: string;

  constructor(
    private projectService: ProjectService,
    private router: Router,
    private route: ActivatedRoute,
    private toastr: ToastrService,
  ) {
    this.courseId = 0;
    this.projectCode = "";
    this.datePickerSettings = {
      bigBanner: true,
      timePicker: true,
      format: "dd-MM-yyyy HH:mm",
    };
  }

  ngOnInit() {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const parentParams = this.route.parent.snapshot.params;
      this.courseId = +parentParams["courseId"]; // (+) converts 'courseId' to a number
      this.projectCode = params["code"];

      this.loading = true;
      this.projectService
        .getProjectDetails(this.courseId, this.projectCode)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result: GetResult<IProjectDetailsModel>) => {
          this.loading = false;
          if (result.success) {
            this.model = result.value;

            if (this.model.teams.length > 0) {
              this.selectedTeamId = this.model.teams[0].id;
            }
          } else {
            this.toastr.error(
              "Could not load project details from API. Message: " +
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


}
