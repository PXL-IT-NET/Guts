import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { GetResult } from "../../util/Result";
import { ToastrService } from 'ngx-toastr';

@Component({
  templateUrl: './project.component.html'
})
export class ProjectComponent implements OnInit, OnDestroy {
  private courseId: number;
  private projectCode: string;

  constructor(private router: Router,
    private route: ActivatedRoute) {
    this.courseId = 0;
    this.projectCode = '';
  }

  ngOnInit() {
    this.route.params.subscribe(params => {

      var parentParams = this.route.parent.snapshot.params;
      this.courseId = +parentParams['courseId']; // (+) converts 'courseId' to a number
      this.projectCode = params['code'];

    });
  }

  ngOnDestroy() {

  }
}
