import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { Subject } from "rxjs";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.Eager,
  templateUrl: "./courseconfig.component.html",
})
export class CourseConfigComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  public courseId: number;

  constructor(private route: ActivatedRoute) {
    this.courseId = 0;
  }

  ngOnInit() {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.courseId = +params["courseId"]; // (+) converts 'courseId' to a number
    });
  }

  ngOnDestroy(): void {}
}
