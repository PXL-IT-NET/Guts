import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LoginModel } from '../../viewmodels/login.model';
import { PostResult} from "../../util/result";


@Component({
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  public model: LoginModel;
  public loading = false;
  public error = '';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authenticationService: AuthService) {
    this.model = new LoginModel();

    route.queryParams.subscribe(params => {
      this.model.loginSessionPublicIdentifier = params['s'];
    });
  }

  ngOnInit() {
    // reset login status
    this.authenticationService.logout();
  }

  public login() {
    this.loading = true;

    this.authenticationService.login(this.model)
      .subscribe(result => this.handleLoginResult(result));
  }

  public cancelSession() {
    if (this.model.loginSessionPublicIdentifier && this.model.loginSessionPublicIdentifier.length > 0) {
      this.authenticationService.cancelLoginSession(this.model.loginSessionPublicIdentifier).subscribe(result => {
        this.router.navigate(['/']);
      });
    }
  }

  private handleLoginResult(result: PostResult) {
    if (result.success) {
      this.router.navigate(['/']);
    } else if (!result.isAuthenticated) {
      this.error = 'Wrong email-password combination';
    } else {
      this.error = result.message;
    }
    this.loading = false;
  }
}
