import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LoginModel } from '../../viewmodels/login.model';
import { AuthHubService } from '../../services/authhub.service';
import { Result } from '../../util/result';


@Component({
  templateUrl: './login.component.html'
})
export class LoginComponent implements OnInit {
  public model: LoginModel;
  public loading = false;
  public error = '';

  public sessionId: string = null;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authenticationService: AuthService,
    private authhubService: AuthHubService) {
    this.model = {
      email: '',
      password: ''
    };

    route.queryParams.subscribe(params => {
      this.sessionId = params['s'];
    });
  }

  ngOnInit() {
    // reset login status
    this.authenticationService.logout();
  }

  login() {
    this.loading = true;

    this.authenticationService.login(this.model).subscribe()

    this.authenticationService.login(this.model)
      .subscribe(result => {
        if (result.success && this.sessionId && this.sessionId.length > 0) {
          this.authhubService.sendToken(this.sessionId, this.authenticationService.getToken()).subscribe(() => {
            this.sessionId = null; //ensure no cancel notification is sent
            this.handleLoginResult(result);
          });
        } else {
          this.handleLoginResult(result);
        }
      });
  }

  private handleLoginResult(result: Result) {
    if (result.success) {
      this.router.navigate(['/']);
    } else {
      this.error = result.message || 'Wrong email-password combination';
    }
    this.loading = false;
  }

  private cancelSession() {
    if (this.sessionId && this.sessionId.length > 0) {
      this.authhubService.cancel(this.sessionId).subscribe(() => {
        this.sessionId = null;
      });
    }
  }

  ngOnDestroy() {
    this.cancelSession();
  }
}
