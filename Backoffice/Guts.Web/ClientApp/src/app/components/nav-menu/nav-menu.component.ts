import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserProfile } from '../../viewmodels/user.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss']
})
export class NavMenuComponent {

  public userProfile: UserProfile;

  constructor(private router: Router,
    private authService: AuthService) {
    this.userProfile = new UserProfile();

    this.authService.getLoggedInState().subscribe((isLoggedIn) => {
      if (isLoggedIn) {
        // retrieve the user profile when logged in
        this.authService.getUserProfile().subscribe(profile => {
          this.userProfile = profile;
        });
      } else {
        // clear the user profile when logged out
        this.userProfile = new UserProfile();
      } 
    });
  }

  public logout() {
    console.log('logging out');
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
