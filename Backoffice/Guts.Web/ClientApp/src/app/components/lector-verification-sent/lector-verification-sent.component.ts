import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { CommonModule } from "@angular/common";
import { Router, RouterModule } from "@angular/router";
import { finalize } from "rxjs";
import { AuthService } from "../../services/auth.service";

interface LectorVerificationNavState {
  email?: string;
  userId?: number | string;
  resendToken?: string;
}

@Component({
  standalone: true,
  imports: [CommonModule, RouterModule],
  selector: "app-lector-verification-sent",
  templateUrl: "./lector-verification-sent.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LectorVerificationSentComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly email = signal<string | null>(null);
  public readonly userId = signal<string | null>(null);
  public readonly resendToken = signal<string | null>(null);
  public readonly loading = signal<boolean>(false);
  public readonly resent = signal<boolean>(false);
  public readonly error = signal<string>("");

  constructor() {
    const navState = (this.router.currentNavigation()?.extras.state ??
      globalThis.history?.state ??
      {}) as LectorVerificationNavState;

    if (navState.email) {
      this.email.set(navState.email);
    }
    if (navState.userId !== undefined && navState.userId !== null) {
      this.userId.set(String(navState.userId));
    }
    if (navState.resendToken) {
      this.resendToken.set(navState.resendToken);
    }
  }

  public canResend(): boolean {
    return Boolean(this.userId() && this.resendToken());
  }

  public resendVerificationEmail(): void {
    const id = this.userId();
    const token = this.resendToken();
    if (!id || !token || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.resent.set(false);
    this.error.set("");

    this.authService
      .resendLectorVerification({ userId: id, resendToken: token })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((result) => {
        if (result.success) {
          this.resent.set(true);
        } else {
          this.error.set(
            result.message ||
              "Could not resend the verification email. Please try logging in again.",
          );
        }
      });
  }
}
