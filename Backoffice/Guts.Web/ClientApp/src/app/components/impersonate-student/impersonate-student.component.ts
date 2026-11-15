import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";
import { finalize } from "rxjs";
import { ToastrService } from "ngx-toastr";
import {
  IImpersonatableUser,
  ImpersonationService,
} from "../../services/impersonation.service";
import { AuthService } from "../../services/auth.service";
import { LoadingOverlayComponent } from "../loading-overlay/loading-overlay.component";
import { PaginationFooterComponent } from "../pagination-footer/pagination-footer.component";
import { calculateTotalPages } from "../../util/paged-result";

@Component({
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LoadingOverlayComponent,
    PaginationFooterComponent,
  ],
  selector: "app-impersonate-student",
  templateUrl: "./impersonate-student.component.html",
  styleUrl: "./impersonate-student.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImpersonateStudentComponent implements OnInit {
  private readonly impersonationService = inject(ImpersonationService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);

  public readonly students = signal<IImpersonatableUser[]>([]);
  public readonly totalCount = signal<number>(0);
  public readonly currentPage = signal<number>(1);
  public readonly pageSize = signal<number | null>(25);
  public readonly loading = signal<boolean>(false);
  public searchTerm = "";

  public readonly totalPages = computed(() =>
    calculateTotalPages(this.totalCount(), this.pageSize()),
  );

  public ngOnInit(): void {
    this.loadUsers();
  }

  public search(): void {
    this.currentPage.set(1);
    this.loadUsers();
  }

  public clearSearchTerm(): void {
    this.searchTerm = "";
    this.search();
  }

  public hasActiveFilters(): boolean {
    return this.searchTerm.trim().length > 0;
  }

  public onPageSizeChange(newSize: number | null): void {
    this.pageSize.set(newSize);
    this.currentPage.set(1);
    this.loadUsers();
  }

  public goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) {
      return;
    }
    this.currentPage.set(page);
    this.loadUsers();
  }

  public loadUsers(): void {
    this.loading.set(true);
    this.impersonationService
      .searchUsers(this.searchTerm, this.currentPage(), this.pageSize())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((res) => {
        if (res.success) {
          const paged = res.value;
          if (
            paged.items.length === 0 &&
            paged.totalCount > 0 &&
            this.currentPage() > 1
          ) {
            this.totalCount.set(paged.totalCount);
            this.currentPage.set(this.totalPages());
            this.loadUsers();
            return;
          }
          this.students.set(paged.items);
          this.totalCount.set(paged.totalCount);
        } else {
          this.toastr.error(res.message || "Failed to load students.", "Error");
        }
      });
  }

  public impersonate(student: IImpersonatableUser): void {
    this.impersonationService.impersonate(student.id).subscribe((res) => {
      if (res.success) {
        this.authService.startImpersonation(res.value);
        void this.router.navigate(["/home"]);
      } else {
        this.toastr.error(
          res.message || "Could not impersonate student.",
          "Error",
        );
      }
    });
  }
}
