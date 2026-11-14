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
import { AdminUserService, IAdminUser } from "../../services/admin-user.service";
import { ImpersonationService } from "../../services/impersonation.service";
import { AuthService } from "../../services/auth.service";
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
  selector: "app-admin-users",
  templateUrl: "./admin-users.component.html",
  styleUrl: "./admin-users.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersComponent implements OnInit {
  private readonly adminUserService = inject(AdminUserService);
  private readonly impersonationService = inject(ImpersonationService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);

  public readonly roleOptions: Array<{ label: string; value: string }> = [
    { label: "All", value: "" },
    { label: "Student", value: "student" },
    { label: "Lector", value: "lector" },
    { label: "Admin", value: "admin" },
  ];

  public readonly pageSizeOptions: Array<{ label: string; value: number | null }> = [
    { label: "25", value: 25 },
    { label: "50", value: 50 },
    { label: "100", value: 100 },
    { label: "All", value: null },
  ];

  public readonly users = signal<IAdminUser[]>([]);
  public readonly totalCount = signal<number>(0);
  public readonly currentPage = signal<number>(1);
  public readonly pageSize = signal<number | null>(25);
  public readonly loading = signal<boolean>(false);

  public searchTerm = "";
  public roleFilter = "";
  public orphanedOnly = false;

  public readonly selectedUserIds = signal<Set<number>>(new Set());
  public readonly userPendingDelete = signal<IAdminUser | null>(null);
  public readonly batchDeletePending = signal<boolean>(false);

  public readonly visibleDeletableUsers = computed(() =>
    this.users().filter((u) => u.canDelete),
  );

  public readonly selectedUsers = computed(() => {
    const selected = this.selectedUserIds();
    return this.users().filter((u) => selected.has(u.id));
  });

  public readonly selectedCount = computed(() => this.selectedUsers().length);

  public readonly allVisibleDeletableSelected = computed(() => {
    const deletable = this.visibleDeletableUsers();
    const selected = this.selectedUserIds();
    return deletable.length > 0 && deletable.every((u) => selected.has(u.id));
  });

  public readonly someVisibleDeletableSelected = computed(() => {
    const deletable = this.visibleDeletableUsers();
    const selected = this.selectedUserIds();
    return (
      !this.allVisibleDeletableSelected() &&
      deletable.some((u) => selected.has(u.id))
    );
  });

  public readonly hasSelectedLector = computed(() =>
    this.selectedUsers().some((u) => this.isLectorUser(u)),
  );

  public readonly totalPages = computed(() => {
    const size = this.pageSize();
    if (size === null || size <= 0) {
      return 1;
    }
    return Math.max(1, Math.ceil(this.totalCount() / size));
  });

  public readonly visiblePages = computed<number[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const maxButtons = 7;
    if (total <= maxButtons) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    let start = Math.max(1, current - 3);
    const end = Math.min(total, start + maxButtons - 1);
    start = Math.max(1, end - maxButtons + 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  public readonly displayRangeLabel = computed(() => {
    const total = this.totalCount();
    const count = this.users().length;
    if (total === 0) {
      return "0 users";
    }
    const size = this.pageSize();
    if (size === null) {
      return `Showing 1-${total} of ${total} users`;
    }
    const start = (this.currentPage() - 1) * size + 1;
    const end = Math.min(total, start + Math.max(0, count - 1));
    return `Showing ${start}-${end} of ${total} users`;
  });

  public ngOnInit(): void {
    this.loadUsers();
  }

  public search(): void {
    this.currentPage.set(1);
    this.loadUsers();
  }

  public selectRoleFilter(role: string): void {
    if (this.roleFilter === role) {
      return;
    }
    this.roleFilter = role;
    this.search();
  }

  public onOrphanedOnlyChange(value: boolean): void {
    this.orphanedOnly = value;
    this.search();
  }

  public clearSearchTerm(): void {
    this.searchTerm = "";
    this.search();
  }

  public resetFilters(): void {
    this.searchTerm = "";
    this.roleFilter = "";
    this.orphanedOnly = false;
    this.search();
  }

  public hasActiveFilters(): boolean {
    return (
      this.searchTerm.trim().length > 0 ||
      this.roleFilter.length > 0 ||
      this.orphanedOnly
    );
  }

  public clearSelection(): void {
    this.selectedUserIds.set(new Set());
    this.batchDeletePending.set(false);
  }

  public getRoleBadgeClass(role: string): string {
    switch (role) {
      case "admin":
        return "text-bg-dark";
      case "lector":
        return "text-bg-warning";
      default:
        return "text-bg-secondary";
    }
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
    this.selectedUserIds.set(new Set());
    this.batchDeletePending.set(false);
    this.adminUserService
      .searchUsers(
        this.searchTerm,
        this.roleFilter,
        this.orphanedOnly,
        this.currentPage(),
        this.pageSize(),
      )
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        if (result.success) {
          const paged = result.value;
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
          this.users.set(paged.items);
          this.totalCount.set(paged.totalCount);
        } else {
          this.toastr.error(result.message || "Failed to load users.", "Error");
        }
      });
  }

  public isLectorUser(user: IAdminUser): boolean {
    return user.roles.includes("lector");
  }

  public getCourseLinksLabel(user: IAdminUser): string {
    const count = user.linkedCourses?.length ?? 0;
    if (count === 0) {
      return "Linked";
    }
    return count === 1 ? "Linked to 1 course" : `Linked to ${count} courses`;
  }

  public getLinkedCoursesTooltip(user: IAdminUser): string | null {
    if (!user.linkedCourses || user.linkedCourses.length === 0) {
      return null;
    }
    return user.linkedCourses.map((c) => `${c.code} - ${c.name}`).join("\n");
  }

  public isSelected(user: IAdminUser): boolean {
    return this.selectedUserIds().has(user.id);
  }

  public toggleUserSelection(user: IAdminUser, checked: boolean): void {
    if (!user.canDelete) {
      return;
    }
    const next = new Set(this.selectedUserIds());
    if (checked) {
      next.add(user.id);
    } else {
      next.delete(user.id);
    }
    this.selectedUserIds.set(next);
  }

  public toggleSelectAllVisible(checked: boolean): void {
    if (checked) {
      this.selectedUserIds.set(
        new Set(this.visibleDeletableUsers().map((u) => u.id)),
      );
    } else {
      this.selectedUserIds.set(new Set());
    }
  }

  public impersonate(user: IAdminUser): void {
    this.impersonationService.impersonate(user.id).subscribe((result) => {
      if (result.success) {
        this.authService.startImpersonation(result.value);
        void this.router.navigate(["/home"]);
      } else {
        this.toastr.error(result.message || "Could not impersonate user.", "Error");
      }
    });
  }

  public requestDelete(user: IAdminUser): void {
    this.batchDeletePending.set(false);
    this.userPendingDelete.set(user);
  }

  public cancelDelete(): void {
    this.userPendingDelete.set(null);
  }

  public confirmDelete(): void {
    const user = this.userPendingDelete();
    if (!user) {
      return;
    }

    this.loading.set(true);
    this.adminUserService
      .deleteUser(user.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        this.userPendingDelete.set(null);
        if (result.success) {
          this.toastr.success(`User '${user.email}' deleted.`, "Success");
          this.loadUsers();
        } else {
          this.toastr.error(result.message || "Could not delete user.", "Error");
        }
      });
  }

  public requestDeleteSelected(): void {
    if (this.selectedCount() === 0) {
      this.toastr.warning("Please select at least one user to delete.", "Warning");
      return;
    }
    this.userPendingDelete.set(null);
    this.batchDeletePending.set(true);
  }

  public cancelBatchDelete(): void {
    this.batchDeletePending.set(false);
  }

  public confirmBatchDelete(): void {
    const ids = this.selectedUsers().map((u) => u.id);
    if (ids.length === 0) {
      return;
    }

    this.loading.set(true);
    this.adminUserService
      .deleteUsers(ids)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        this.batchDeletePending.set(false);
        if (result.success) {
          this.selectedUserIds.set(new Set());
          this.toastr.success(
            `Deleted ${result.value.deletedCount} user(s).`,
            "Success",
          );
          this.loadUsers();
        } else {
          this.toastr.error(
            result.message || "Could not delete selected users.",
            "Error",
          );
        }
      });
  }

  public promoteToAdmin(user: IAdminUser): void {
    this.loading.set(true);
    this.adminUserService
      .promoteToAdmin(user.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        if (result.success) {
          this.toastr.success(`'${user.email}' is now an administrator.`, "Success");
          this.loadUsers();
        } else {
          this.toastr.error(
            result.message || "Could not promote user to administrator.",
            "Error",
          );
        }
      });
  }

  public demoteFromAdmin(user: IAdminUser): void {
    this.loading.set(true);
    this.adminUserService
      .demoteFromAdmin(user.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        if (result.success) {
          this.toastr.success(
            `Removed administrator role from '${user.email}'.`,
            "Success",
          );
          this.loadUsers();
        } else {
          this.toastr.error(
            result.message || "Could not remove administrator role.",
            "Error",
          );
        }
      });
  }
}
