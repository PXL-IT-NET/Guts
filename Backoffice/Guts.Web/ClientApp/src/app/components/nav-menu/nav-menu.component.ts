import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { UserProfile } from "../../viewmodels/user.model";
import { PeriodService } from "src/app/services/period.service";
import { PeriodProvider } from "src/app/services/period.provider";
import { IPeriodModel, PeriodModel } from "src/app/viewmodels/period.model";

@Component({
  standalone: false,
  selector: "app-nav-menu",
  templateUrl: "./nav-menu.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ["./nav-menu.component.scss"],
})
export class NavMenuComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  private readonly _userProfile = signal<UserProfile>(new UserProfile());
  public get userProfile(): UserProfile {
    return this._userProfile();
  }
  public set userProfile(v: UserProfile) {
    this._userProfile.set(v);
  }

  private readonly _allPeriods = signal<IPeriodModel[]>([]);
  public get allPeriods(): IPeriodModel[] {
    return this._allPeriods();
  }
  public set allPeriods(v: IPeriodModel[]) {
    this._allPeriods.set(v);
  }

  private readonly _selectedPeriod = signal<IPeriodModel>(new PeriodModel());
  public get selectedPeriod(): IPeriodModel {
    return this._selectedPeriod();
  }
  public set selectedPeriod(v: IPeriodModel) {
    this._selectedPeriod.set(v);
  }

  constructor(
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly periodService: PeriodService,
    private readonly periodProvider: PeriodProvider,
  ) {}

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
                  this.periodProvider.period = this.allPeriods.at(-1)!;
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


}
