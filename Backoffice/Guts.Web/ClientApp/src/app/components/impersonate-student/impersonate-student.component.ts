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
import { ToastrService } from "ngx-toastr";
import {
  IImpersonatableUser,
  ImpersonationService,
} from "../../services/impersonation.service";
import { AuthService } from "../../services/auth.service";

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  selector: "app-impersonate-student",
  templateUrl: "./impersonate-student.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImpersonateStudentComponent implements OnInit {
  private readonly impersonationService = inject(ImpersonationService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastrService);

  public readonly students = signal<IImpersonatableUser[]>([]);
  public searchTerm = "";

  public ngOnInit(): void {
    this.search();
  }

  public search(): void {
    this.impersonationService.searchUsers(this.searchTerm).subscribe((res) => {
      if (res.success) {
        this.students.set(res.value);
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
