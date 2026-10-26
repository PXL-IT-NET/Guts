import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { Component, OnInit, ChangeDetectionStrategy, DestroyRef, inject, Input } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./courseconfig.component.html",
})
export class CourseConfigComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  @Input() public courseId: number = 0;

  constructor(private readonly route: ActivatedRoute) {}

  ngOnInit() {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      if (params?.["courseId"]) {
        this.courseId = +params["courseId"];
      }
    });
  }


}
