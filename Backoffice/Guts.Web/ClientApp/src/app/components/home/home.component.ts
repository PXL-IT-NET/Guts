import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { AuthService } from "src/app/services/auth.service";
import { UserProfile } from "src/app/viewmodels/user.model";

@Component({
  standalone: false,
  selector: "app-home",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./home.component.html",
})
export class HomeComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);

  private _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService
      .getLoggedInState()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isLoggedIn) => {
        if (isLoggedIn) {
          // retrieve the user profile when logged in
          this.authService
            .getUserProfile()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((profile) => {
              this.userProfile = profile;
            });
        } else {
          // clear the user profile when logged out
          this.userProfile = new UserProfile();
        }
      });
  }

  ngOnDestroy(): void {}
}
