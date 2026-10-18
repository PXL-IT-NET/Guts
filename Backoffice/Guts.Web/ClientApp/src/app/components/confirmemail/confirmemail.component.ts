import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { Params, ActivatedRoute } from "@angular/router";
import { AuthService } from "../../services/auth.service";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./confirmemail.component.html",
})
export class ConfirmEmailComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);

  private _message = signal<string>("Confirming email...");
  public get message(): string {
    return this._message();
  }
  public set message(v: string) {
    this._message.set(v);
  }

  private _confirmed = signal<boolean>(false);
  public get confirmed(): boolean {
    return this._confirmed();
  }
  public set confirmed(v: boolean) {
    this._confirmed.set(v);
  }

  private _loading = signal<boolean>(true);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  constructor(
    private activatedRoute: ActivatedRoute,
    private authenticationService: AuthService,
  ) {}

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
