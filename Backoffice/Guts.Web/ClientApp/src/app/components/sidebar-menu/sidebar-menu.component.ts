import { ChangeDetectorRef, Component, OnDestroy } from "@angular/core";
import { AuthService } from "../../services/auth.service";
import { Router } from "@angular/router";
import { CourseService } from "../../services/course.service";
import { ICourseModel } from "../../viewmodels/course.model";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  selector: "app-sidebar-menu",
  templateUrl: "./sidebar-menu.component.html",
  styleUrls: ["./sidebar-menu.component.scss"],
})
export class SidebarMenuComponent implements OnDestroy {
  private destroy$ = new Subject<void>();
  public courses: ICourseModel[];

  constructor(
    private authService: AuthService,
    private router: Router,
    private courseService: CourseService,
    private cdr: ChangeDetectorRef,
  ) {
    this.courses = [];

    this.authService
      .getLoggedInState()
      .pipe(takeUntil(this.destroy$))
      .subscribe((isLoggedIn) => {
        if (isLoggedIn) {
          this.loadCourses();
        } else {
          this.courses = [];
        }

        this.cdr.detectChanges();
      });
  }

  public isCourseRouteActive(courseId: number) {
    return (
      this.router.url.includes("/courses/" + courseId + "/") ||
      this.router.url.endsWith("/courses/" + courseId)
    );
  }

  private loadCourses() {
    this.courseService
      .getCourses()
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        if (result.success) {
          this.courses = result.value;
        }

        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
