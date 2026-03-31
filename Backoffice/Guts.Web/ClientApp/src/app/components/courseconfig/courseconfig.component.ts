import { ChangeDetectorRef, Component, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

@Component({
  standalone: false,
  templateUrl: "./courseconfig.component.html",
})
export class CourseConfigComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  public courseId: number;

  constructor(
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
  ) {
    this.courseId = 0;
  }

  ngOnInit() {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      this.courseId = +params["courseId"]; // (+) converts 'courseId' to a number
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
