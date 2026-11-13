import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
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

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingOverlayComponent],
  selector: "app-admin-users",
  templateUrl: "./admin-users.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminUsersComponent implements OnInit {
  private readonly adminUserService = inject(AdminUserService);
  private readonly impersonationService = inject(ImpersonationService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);

  public readonly users = signal<IAdminUser[]>([]);
  public readonly loading = signal<boolean>(false);

  public searchTerm = "";
  public roleFilter = "";
  public OrphanedOnly = false;

  public readonly userPendingDelete = signal<IAdminUser | null>(null);
  public readonly bulkDeletePending = signal<boolean>(false);

  public ngOnInit(): void {
    this.search();
  }

  public search(): void {
    this.loading.set(true);
    this.adminUserService
      .searchUsers(this.searchTerm, this.roleFilter, this.OrphanedOnly)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        if (result.success) {
          this.users.set(result.value);
        } else {
          this.toastr.error(result.message || "Failed to load users.", "Error");
        }
      });
  }

  public isLectorUser(user: IAdminUser): boolean {
    return user.roles.includes("lector");
  }

  public hasOrphanedLector(): boolean {
    return this.users().some((u) => u.canDelete && this.isLectorUser(u));
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
    this.bulkDeletePending.set(false);
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
          this.search();
        } else {
          this.toastr.error(result.message || "Could not delete user.", "Error");
        }
      });
  }

  public requestBulkDelete(): void {
    this.userPendingDelete.set(null);
    this.bulkDeletePending.set(true);
  }

  public cancelBulkDelete(): void {
    this.bulkDeletePending.set(false);
  }

  public confirmBulkDelete(): void {
    this.loading.set(true);
    this.adminUserService
      .deleteAllOrphanedUsers()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        this.bulkDeletePending.set(false);
        if (result.success) {
          this.toastr.success(
            `Removed ${result.value.deletedCount} Orphaned user(s).`,
            "Success",
          );
          this.search();
        } else {
          this.toastr.error(
            result.message || "Could not remove Orphaned users.",
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
          this.search();
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
          this.search();
        } else {
          this.toastr.error(
            result.message || "Could not remove administrator role.",
            "Error",
          );
        }
      });
  }
}
