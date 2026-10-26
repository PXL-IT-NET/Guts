import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, inject, signal } from "@angular/core";
import { Router, ActivatedRoute } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { LoginModel } from "../../viewmodels/login.model";
import { PostResult } from "../../util/result";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./login.component.html",
})
export class LoginComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  public model: LoginModel;

  private readonly _loading = signal<boolean>(false);
  public get loading(): boolean {
    return this._loading();
  }
  public set loading(v: boolean) {
    this._loading.set(v);
  }

  private readonly _error = signal<string>("");
  public get error(): string {
    return this._error();
  }
  public set error(v: string) {
    this._error.set(v);
  }

  constructor(
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly authenticationService: AuthService,
  ) {
    this.model = new LoginModel();

    route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.model.loginSessionPublicIdentifier = params["s"];
    });
  }

  ngOnInit() {
    // reset login status
    this.authenticationService.logout();
  }

  public login() {
    this.loading = true;

    this.authenticationService
      .login(this.model)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.handleLoginResult(result);
      });
  }

  public cancelSession() {
    if (
      this.model.loginSessionPublicIdentifier &&
      this.model.loginSessionPublicIdentifier.length > 0
    ) {
      this.authenticationService
        .cancelLoginSession(this.model.loginSessionPublicIdentifier)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((result) => {
          void this.router.navigate(["/"]);
        });
    }
  }

  private handleLoginResult(result: PostResult) {
    if (result.success) {
      void this.router.navigate(["/"]);
    } else if (!result.isAuthenticated) {
      this.error = "Wrong email-password combination";
    } else {
      this.error = result.message;
    }
    this.loading = false;
  }


}
