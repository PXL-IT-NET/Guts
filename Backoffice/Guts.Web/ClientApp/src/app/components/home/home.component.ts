import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { takeUntil } from "rxjs/internal/operators/takeUntil";
import { Subject } from "rxjs/internal/Subject";
import { AuthService } from "src/app/services/auth.service";
import { UserProfile } from "src/app/viewmodels/user.model";

@Component({
  standalone: false,
  selector: "app-home",
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: "./home.component.html",
})
export class HomeComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);

  public userProfile: UserProfile;

  constructor(private authService: AuthService) {
    this.userProfile = new UserProfile();
  }

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
