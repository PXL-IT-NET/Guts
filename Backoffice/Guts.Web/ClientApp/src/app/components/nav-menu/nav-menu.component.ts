import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { UserProfile } from "../../viewmodels/user.model";
import { PeriodService } from "src/app/services/period.service";
import { PeriodProvider } from "src/app/services/period.provider";
import { IPeriodModel, PeriodModel } from "src/app/viewmodels/period.model";
import { Subject } from "rxjs";

@Component({
  standalone: false,
  selector: "app-nav-menu",
  templateUrl: "./nav-menu.component.html",
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ["./nav-menu.component.scss"],
})
export class NavMenuComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  public userProfile: UserProfile;
  public allPeriods: IPeriodModel[];
  public selectedPeriod: IPeriodModel;

  constructor(
    private router: Router,
    private authService: AuthService,
    private periodService: PeriodService,
    private periodProvider: PeriodProvider,
  ) {
    this.userProfile = new UserProfile();
    this.allPeriods = [];
    this.selectedPeriod = new PeriodModel();
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

          this.periodService
            .getAll()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((result) => {
              if (result.success) {
                if (result.value.length > 0) {
                  //find the index of the active period and remove all periods after it
                  let pastAndPresentPeriods = result.value;
                  let activePeriodIndex = pastAndPresentPeriods.findIndex(
                    (p) => p.isActive,
                  );
                  if (activePeriodIndex !== -1) {
                    pastAndPresentPeriods = pastAndPresentPeriods.slice(
                      0,
                      activePeriodIndex + 1,
                    );
                  }
                  this.allPeriods = pastAndPresentPeriods;
                }

                if (this.allPeriods.length > 0) {
                  this.periodProvider.period =
                    this.allPeriods[this.allPeriods.length - 1];
                }
              }
            });
        } else {
          // clear the user profile when logged out
          this.userProfile = new UserProfile();
        }
      });

    this.periodProvider.period$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((period) => {
        if (period) {
          this.selectedPeriod = period;
        }
      });
  }

  public logout() {
    console.log("logging out");
    this.authService.logout();
    void this.router.navigate(["/"]);
  }

  public selectPeriod(period: IPeriodModel) {
    this.periodProvider.period = period;
  }

  ngOnDestroy(): void {}
}
