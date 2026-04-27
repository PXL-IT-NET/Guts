import { ChangeDetectorRef, Component, OnDestroy, OnInit } from "@angular/core";
import { takeUntil } from "rxjs/internal/operators/takeUntil";
import { Subject } from "rxjs/internal/Subject";
import { AuthService } from "src/app/services/auth.service";
import { UserProfile } from "src/app/viewmodels/user.model";

@Component({
  standalone: false,
  selector: "app-home",
  templateUrl: "./home.component.html",
})
export class HomeComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  public userProfile: UserProfile;

  constructor(
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
  ) {
    this.userProfile = new UserProfile();
  }

  ngOnInit(): void {
    this.authService
      .getLoggedInState()
      .pipe(takeUntil(this.destroy$))
      .subscribe((isLoggedIn) => {
        if (isLoggedIn) {
          // retrieve the user profile when logged in
          this.authService
            .getUserProfile()
            .pipe(takeUntil(this.destroy$))
            .subscribe((profile) => {
              this.userProfile = profile;

              this.cdr.detectChanges();
            });
        } else {
          // clear the user profile when logged out
          this.userProfile = new UserProfile();
        }

        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
