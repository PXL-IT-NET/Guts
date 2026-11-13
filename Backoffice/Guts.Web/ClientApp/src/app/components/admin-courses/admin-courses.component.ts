import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { finalize } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { CourseService } from "../../services/course.service";
import { ICourseModel } from "../../viewmodels/course.model";
import { LoadingOverlayComponent } from "../loading-overlay/loading-overlay.component";
import { DangerDeleteModalComponent } from "../danger-delete-modal/danger-delete-modal.component";

@Component({
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LoadingOverlayComponent,
    DangerDeleteModalComponent,
  ],
  selector: "app-admin-courses",
  templateUrl: "./admin-courses.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminCoursesComponent implements OnInit {
  private readonly courseService = inject(CourseService);
  private readonly toastr = inject(ToastrService);

  public readonly courses = signal<ICourseModel[]>([]);
  public readonly loading = signal<boolean>(false);

  public readonly editingCourseId = signal<number | null>(null);
  public formCode = "";
  public formName = "";

  public readonly coursePendingDelete = signal<ICourseModel | null>(null);

  public ngOnInit(): void {
    this.loadCourses();
  }

  public loadCourses(): void {
    this.loading.set(true);
    this.courseService
      .getCourses()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        if (result.success) {
          this.courses.set(result.value);
        } else {
          this.toastr.error(result.message || "Failed to load courses.", "Error");
        }
      });
  }

  public startCreate(): void {
    this.editingCourseId.set(0);
    this.formCode = "";
    this.formName = "";
  }

  public startEdit(course: ICourseModel): void {
    this.editingCourseId.set(course.id);
    this.formCode = course.code;
    this.formName = course.name;
  }

  public cancelForm(): void {
    this.editingCourseId.set(null);
  }

  public saveCourse(): void {
    const id = this.editingCourseId();
    if (id === null) {
      return;
    }

    this.loading.set(true);
    const payload = { code: this.formCode, name: this.formName };
    if (id === 0) {
      this.courseService
        .createCourse(payload)
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe((result) => {
          if (result.success) {
            this.editingCourseId.set(null);
            this.toastr.success("Course created.", "Success");
            this.loadCourses();
          } else {
            this.toastr.error(result.message || "Could not create course.", "Error");
          }
        });
    } else {
      this.courseService
        .updateCourse(id, payload)
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe((result) => {
          if (result.success) {
            this.editingCourseId.set(null);
            this.toastr.success("Course updated.", "Success");
            this.loadCourses();
          } else {
            this.toastr.error(result.message || "Could not update course.", "Error");
          }
        });
    }
  }

  public requestDelete(course: ICourseModel): void {
    this.coursePendingDelete.set(course);
  }

  public cancelDelete(): void {
    this.coursePendingDelete.set(null);
  }

  public confirmDelete(): void {
    const course = this.coursePendingDelete();
    if (!course) {
      return;
    }

    this.loading.set(true);
    this.courseService
      .deleteCourse(course.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        this.coursePendingDelete.set(null);
        if (result.success) {
          this.toastr.success(`Course '${course.name}' deleted.`, "Success");
          this.loadCourses();
        } else {
          this.toastr.error(result.message || "Could not delete course.", "Error");
        }
      });
  }
}
