import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnDestroy, OnInit, ChangeDetectionStrategy, DestroyRef, inject, Input } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./courseconfig.component.html",
})
export class CourseConfigComponent implements OnInit, OnDestroy {
  private destroyRef = inject(DestroyRef);
  @Input() public courseId: number = 0;

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      if (params && params["courseId"]) {
        this.courseId = +params["courseId"];
      }
    });
  }

  ngOnDestroy(): void {}
}
