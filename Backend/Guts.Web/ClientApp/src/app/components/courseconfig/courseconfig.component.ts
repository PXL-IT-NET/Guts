import { Component, OnInit, OnDestroy  } from '@angular/core';
import { CourseService } from '../../services/course.service';
import { AuthService } from "../../services/auth.service";
import { ActivatedRoute, Router } from '@angular/router';
import { UserProfile } from "../../viewmodels/user.model";
import { Subscription } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

@Component({
  templateUrl: './courseconfig.component.html'
})
export class CourseConfigComponent implements OnInit, OnDestroy  {
  public loading: boolean = false;
  public userProfile: UserProfile;

  private userProfileSubscription: Subscription;

  constructor(private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService,
    private authService: AuthService,
    private toastr: ToastrService) {

  }

  ngOnInit() {

    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
    });

    this.route.params.subscribe(params => {
      let courseId = +params['courseId']; // (+) converts 'courseId' to a number

      this.loading = true;
      this.loading = false;
    });
  }

  ngOnDestroy() {
    this.userProfileSubscription.unsubscribe();
  }
}
