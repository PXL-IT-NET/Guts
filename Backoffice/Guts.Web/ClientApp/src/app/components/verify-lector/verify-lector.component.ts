import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { CommonModule } from "@angular/common";
import { ActivatedRoute, Params, RouterModule } from "@angular/router";
import { finalize } from "rxjs";
import { AuthService } from "../../services/auth.service";

@Component({
  standalone: true,
  imports: [CommonModule, RouterModule],
  selector: "app-verify-lector",
  templateUrl: "./verify-lector.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyLectorComponent implements OnInit {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly message = signal<string>("Verifying your lector account...");
  public readonly verified = signal<boolean>(false);
  public readonly loading = signal<boolean>(true);

  public ngOnInit(): void {
    this.activatedRoute.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params: Params) => {
        const userId = params["userId"] ?? "";
        const token = params["token"] ?? "";

        this.loading.set(true);
        this.authService
          .verifyLector({ userId, token })
          .pipe(
            takeUntilDestroyed(this.destroyRef),
            finalize(() => this.loading.set(false)),
          )
          .subscribe((result) => {
            this.verified.set(result.success);
            if (result.success) {
              this.message.set(
                "Your PXL email address has been verified and your lector account is now unlocked.",
              );
            } else {
              this.message.set(
                result.message ||
                  "The verification link is invalid or has expired. Please log in again to receive a new verification email.",
              );
            }
          });
      });
  }
}
