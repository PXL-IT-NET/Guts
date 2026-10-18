import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { Router, ActivatedRoute } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { LoginModel } from "../../viewmodels/login.model";
import { PostResult } from "../../util/result";
import { Subject } from "rxjs";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: "./login.component.html",
})
export class LoginComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  public model: LoginModel;
  public loading = false;
  public error = "";

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authenticationService: AuthService,
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

  ngOnDestroy(): void {}
}
