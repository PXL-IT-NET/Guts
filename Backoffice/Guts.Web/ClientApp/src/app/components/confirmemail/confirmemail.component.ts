import { ChangeDetectorRef, Component, OnDestroy, OnInit } from "@angular/core";
import { Params, ActivatedRoute } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  templateUrl: "./confirmemail.component.html",
})
export class ConfirmEmailComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  public message: string;
  public confirmed: boolean;
  public loading: boolean;

  constructor(
    private activatedRoute: ActivatedRoute,
    private authenticationService: AuthService,
    private cdr: ChangeDetectorRef,
  ) {
    this.message = "Confirming email...";
    this.confirmed = false;
    this.loading = true;
  }

  ngOnInit() {
    this.activatedRoute.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe((params: Params) => {
        let model = {
          userId: params["userId"],
          token: params["token"],
        };

        this.authenticationService
          .confirmEmail(model)
          .pipe(takeUntil(this.destroy$))
          .subscribe((result) => {
            this.confirmed = result.success;
            if (this.confirmed) {
              this.message =
                "Your email address has been succesfully confirmed. Your registration is complete.";
            } else {
              this.message =
                result.message ||
                "Something went wrong while validating your email address.";
            }
            this.loading = false;

            this.cdr.detectChanges();
          });
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
