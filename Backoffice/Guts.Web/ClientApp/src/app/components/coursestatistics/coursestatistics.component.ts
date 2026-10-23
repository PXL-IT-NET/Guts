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

@Component({
  standalone: false,
  selector: "app-course-statistics",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./coursestatistics.component.html",
})
export class CourseStatisticsComponent implements OnInit, OnDestroy {
  @Input() public courseId: number = 0;

  private _model = signal<ICourseStatisticsModel | null>(null);
  public get model(): ICourseStatisticsModel | null {
    return this._model();
  }
  public set model(v: ICourseStatisticsModel | null) {
    this._model.set(v);
  }

  private _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private _selectedPeriod = signal<IPeriodModel | null>(null);
  public get selectedPeriod(): IPeriodModel | null {
    return this._selectedPeriod();
  }
  public set selectedPeriod(v: IPeriodModel | null) {
    this._selectedPeriod.set(v);
  }

  private _sortField = signal<"firstName" | "lastName">("lastName");
  public get sortField(): "firstName" | "lastName" {
    return this._sortField();
  }
  public set sortField(v: "firstName" | "lastName") {
    this._sortField.set(v);
  }

  private _sortDirection = signal<"asc" | "desc">("asc");
  public get sortDirection(): "asc" | "desc" {
    return this._sortDirection();
  }
  public set sortDirection(v: "asc" | "desc") {
    this._sortDirection.set(v);
  }

  private _expandedStudentIds = signal<Set<number>>(new Set());
  public get expandedStudentIds(): Set<number> {
    return this._expandedStudentIds();
  }

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private periodSubscription?: Subscription;
  private userProfileSubscription?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService,
    private authService: AuthService,
    private periodProvider: PeriodProvider,
    private toastr: ToastrService,
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
          return;
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
      let primaryComparison: number;
      let secondaryComparison: number;

      if (field === "firstName") {
        primaryComparison = (a.firstName || "").localeCompare(
          b.firstName || "",
          undefined,
          { sensitivity: "base" },
        );
        secondaryComparison = (a.lastName || "").localeCompare(
          b.lastName || "",
          undefined,
          { sensitivity: "base" },
        );
      } else {
        primaryComparison = (a.lastName || "").localeCompare(
          b.lastName || "",
          undefined,
          { sensitivity: "base" },
        );
        secondaryComparison = (a.firstName || "").localeCompare(
          b.firstName || "",
          undefined,
          { sensitivity: "base" },
        );
      }

      if (primaryComparison !== 0) {
        return primaryComparison * direction;
      }
      return secondaryComparison * direction;
    });
  }

  public setSort(field: "firstName" | "lastName"): void {
    if (this.sortField === field) {
      this.sortDirection = this.sortDirection === "asc" ? "desc" : "asc";
    } else {
      this.sortField = field;
      this.sortDirection = "asc";
    }
  }

  public getSortIcon(field: "firstName" | "lastName"): string {
    if (this.sortField !== field) {
      return "bi-arrow-down-up text-muted";
    }
    return this.sortDirection === "asc"
      ? "bi-sort-alpha-down text-primary"
      : "bi-sort-alpha-up-alt text-primary";
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

