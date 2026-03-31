import { ChangeDetectorRef, Component, OnDestroy, OnInit } from "@angular/core";
import { Router, ActivatedRoute } from "@angular/router";
import { AuthService } from "../../services/auth.service";
import { LoginModel } from "../../viewmodels/login.model";
import { PostResult } from "../../util/result";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  templateUrl: "./login.component.html",
})
export class LoginComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  public model: LoginModel;
  public loading = false;
  public error = "";

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authenticationService: AuthService,
    private cdr: ChangeDetectorRef,
  ) {
    this.model = new LoginModel();

    route.queryParams.pipe(takeUntil(this.destroy$)).subscribe((params) => {
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
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        this.handleLoginResult(result);
        this.cdr.detectChanges();
      });
  }

  public cancelSession() {
    if (
      this.model.loginSessionPublicIdentifier &&
      this.model.loginSessionPublicIdentifier.length > 0
    ) {
      this.authenticationService
        .cancelLoginSession(this.model.loginSessionPublicIdentifier)
        .pipe(takeUntil(this.destroy$))
        .subscribe((result) => {
          this.router.navigate(["/"]);

          this.cdr.detectChanges();
        });
    }
  }

  private handleLoginResult(result: PostResult) {
    if (result.success) {
      this.router.navigate(["/"]);
    } else if (!result.isAuthenticated) {
      this.error = "Wrong email-password combination";
    } else {
      this.error = result.message;
    }
    this.loading = false;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
