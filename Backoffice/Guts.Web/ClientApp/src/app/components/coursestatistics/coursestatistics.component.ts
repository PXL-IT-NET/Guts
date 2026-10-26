import {
  Component,
  OnInit,
  OnDestroy,
  Input,
  ChangeDetectionStrategy,
  signal,
} from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Subscription } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { CourseService } from "../../services/course.service";
import { AuthService } from "../../services/auth.service";
import { PeriodProvider } from "../../services/period.provider";
import {
  ICourseStatisticsModel,
  ICourseStudentStatisticsModel,
} from "../../viewmodels/course.model";
import { IPeriodModel } from "../../viewmodels/period.model";
import { UserProfile } from "../../viewmodels/user.model";

export type CourseStatisticsSortField =
  | "lastName"
  | "firstName"
  | "assignmentsWorkedOn"
  | "completionPercentage";

@Component({
  standalone: false,
  selector: "app-course-statistics",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./coursestatistics.component.html",
})
export class CourseStatisticsComponent implements OnInit, OnDestroy {
  @Input() public courseId: number = 0;

  private readonly _model = signal<ICourseStatisticsModel | null>(null);
  public get model(): ICourseStatisticsModel | null {
    return this._model();
  }
  public set model(v: ICourseStatisticsModel | null) {
    this._model.set(v);
  }

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private readonly _selectedPeriod = signal<IPeriodModel | null>(null);
  public get selectedPeriod(): IPeriodModel | null {
    return this._selectedPeriod();
  }
  public set selectedPeriod(v: IPeriodModel | null) {
    this._selectedPeriod.set(v);
  }

  private readonly _sortField = signal<CourseStatisticsSortField>("lastName");
  public get sortField(): CourseStatisticsSortField {
    return this._sortField();
  }
  public set sortField(v: CourseStatisticsSortField) {
    this._sortField.set(v);
  }

  private readonly _sortDirection = signal<"asc" | "desc">("asc");
  public get sortDirection(): "asc" | "desc" {
    return this._sortDirection();
  }
  public set sortDirection(v: "asc" | "desc") {
    this._sortDirection.set(v);
  }

  private readonly _expandedStudentIds = signal<Set<number>>(new Set());
  public get expandedStudentIds(): Set<number> {
    return this._expandedStudentIds();
  }

  private readonly _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private periodSubscription?: Subscription;
  private userProfileSubscription?: Subscription;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly courseService: CourseService,
    private readonly authService: AuthService,
    private readonly periodProvider: PeriodProvider,
    private readonly toastr: ToastrService,
  ) {}

  public ngOnInit(): void {
    if (!this.courseId || this.courseId <= 0) {
      const idFromRoute = this.route.snapshot.paramMap.get("courseId");
      if (idFromRoute) {
        this.courseId = +idFromRoute;
      }
    }

    this.userProfileSubscription = this.authService
      .getUserProfile()
      .subscribe((profile) => {
        this.userProfile = profile;
        if (!profile.isLector()) {
          this.toastr.error("Only lectors can view course statistics.");
          void this.router.navigate(["/courses", this.courseId]);
        }
      });

    this.periodSubscription = this.periodProvider.period$.subscribe(
      (period) => {
        this.selectedPeriod = period;
        if (this.courseId > 0) {
          this.loadStatistics();
        }
      },
    );
  }

  public ngOnDestroy(): void {
    this.periodSubscription?.unsubscribe();
    this.userProfileSubscription?.unsubscribe();
  }

  public loadStatistics(): void {
    if (!this.courseId || this.courseId <= 0) return;

    this.loading = true;
    const periodId = this.selectedPeriod?.id ?? 0;

    this.courseService
      .getCourseStatistics(this.courseId, periodId)
      .subscribe((result) => {
        this.loading = false;
        if (result.success) {
          this.model = result.value;
        } else {
          this.toastr.error(
            result.message || "Failed to load course statistics.",
            "Error",
          );
        }
      });
  }

  public get sortedStudents(): ICourseStudentStatisticsModel[] {
    const students = this.model?.students ?? [];
    if (students.length === 0) return [];

    const field = this.sortField;
    const direction = this.sortDirection === "asc" ? 1 : -1;

    return [...students].sort((a, b) => {
      let comparison = 0;
      switch (field) {
        case "lastName":
          comparison = (a.lastName || "").localeCompare(
            b.lastName || "",
            undefined,
            { sensitivity: "base" },
          );
          if (comparison === 0) {
            comparison = (a.firstName || "").localeCompare(
              b.firstName || "",
              undefined,
              { sensitivity: "base" },
            );
          }
          break;
        case "firstName":
          comparison = (a.firstName || "").localeCompare(
            b.firstName || "",
            undefined,
            { sensitivity: "base" },
          );
          if (comparison === 0) {
            comparison = (a.lastName || "").localeCompare(
              b.lastName || "",
              undefined,
              { sensitivity: "base" },
            );
          }
          break;
        case "assignmentsWorkedOn":
          comparison = a.assignmentsWorkedOn - b.assignmentsWorkedOn;
          if (comparison === 0) {
            comparison = (a.lastName || "").localeCompare(
              b.lastName || "",
              undefined,
              { sensitivity: "base" },
            );
          }
          break;
        case "completionPercentage":
          comparison = a.completionPercentage - b.completionPercentage;
          if (comparison === 0) {
            comparison = (a.lastName || "").localeCompare(
              b.lastName || "",
              undefined,
              { sensitivity: "base" },
            );
          }
          break;
      }
      return comparison * direction;
    });
  }

  public setSort(field: CourseStatisticsSortField): void {
    if (this.sortField === field) {
      this.sortDirection = this.sortDirection === "asc" ? "desc" : "asc";
    } else {
      this.sortField = field;
      this.sortDirection = "asc";
    }
  }

  public getSortIcon(field: CourseStatisticsSortField): string {
    if (this.sortField !== field) {
      return "bi-arrow-down-up text-muted";
    }
    return this.sortDirection === "asc"
      ? "bi-sort-down text-primary"
      : "bi-sort-up-alt text-primary";
  }

  public toggleStudent(studentId: number): void {
    const updated = new Set(this.expandedStudentIds);
    if (updated.has(studentId)) {
      updated.delete(studentId);
    } else {
      updated.add(studentId);
    }
    this._expandedStudentIds.set(updated);
  }

  public isExpanded(studentId: number): boolean {
    return this.expandedStudentIds.has(studentId);
  }

  public getProgressBarClass(percentage: number): string {
    if (percentage >= 100) return "bg-success";
    if (percentage >= 50) return "bg-primary";
    if (percentage > 0) return "bg-warning";
    return "bg-secondary";
  }
}
