import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { UserProfile } from "../../viewmodels/user.model";
import { PeriodService } from "src/app/services/period.service";
import { PeriodProvider } from "src/app/services/period.provider";
import { IPeriodModel, PeriodModel } from "src/app/viewmodels/period.model";

@Component({
  selector: "app-nav-menu",
  templateUrl: "./nav-menu.component.html",
  styleUrls: ["./nav-menu.component.scss"],
})
export class NavMenuComponent {
  public userProfile: UserProfile;
  public allPeriods: IPeriodModel[];
  public selectedPeriod: IPeriodModel;

  constructor(
    private router: Router,
    private authService: AuthService,
    private periodService: PeriodService,
    private periodProvider: PeriodProvider
  ) {
    this.userProfile = new UserProfile();
    this.allPeriods = [];
    this.selectedPeriod = new PeriodModel();

    this.authService.getLoggedInState().subscribe((isLoggedIn) => {
      if (isLoggedIn) {
        // retrieve the user profile when logged in
        this.authService.getUserProfile().subscribe((profile) => {
          this.userProfile = profile;
        });

        this.periodService.getAll().subscribe((result) => {
          if (result.success) {
            this.allPeriods = result.value;
            if (this.allPeriods.length > 0) {
              const lastPeriod : IPeriodModel = this.allPeriods[this.allPeriods.length - 1];
              this.periodProvider.period = lastPeriod;
            }
          }
        });
      } else {
        // clear the user profile when logged out
        this.userProfile = new UserProfile();
      }
    });

    this.periodProvider.period$.subscribe((period) => {
      if (period) {
        this.selectedPeriod = period;
      }   
    });
  }

  public logout() {
    console.log("logging out");
    this.authService.logout();
    this.router.navigate(["/"]);
  }

  public selectPeriod(period: IPeriodModel) {
    this.periodProvider.period = period;
  }
}
