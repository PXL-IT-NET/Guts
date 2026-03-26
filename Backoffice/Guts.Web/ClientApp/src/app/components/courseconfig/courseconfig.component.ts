import { Component, OnInit, ChangeDetectorRef } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
  standalone: false,
  templateUrl: "./courseconfig.component.html",
})
export class CourseConfigComponent implements OnInit {
  public courseId: number;

  constructor(
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {
    this.courseId = 0;
  }

  ngOnInit() {
    this.route.params.subscribe((params) => {
      this.courseId = +params["courseId"]; // (+) converts 'courseId' to a number
    });
  }
}
