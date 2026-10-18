import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { Params, ActivatedRoute } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { Subject } from "rxjs";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: "./confirmemail.component.html",
})
export class ConfirmEmailComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  public message: string;
  public confirmed: boolean;
  public loading: boolean;

  constructor(
    private activatedRoute: ActivatedRoute,
    private authenticationService: AuthService,
  ) {
    this.message = "Confirming email...";
    this.confirmed = false;
    this.loading = true;
  }

  ngOnInit() {
    this.activatedRoute.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params: Params) => {
        let model = {
          userId: params["userId"],
          token: params["token"],
        };

        this.authenticationService
          .confirmEmail(model)
          .pipe(takeUntilDestroyed(this.destroyRef))
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
          });
      });
  }

  ngOnDestroy(): void {}
}
