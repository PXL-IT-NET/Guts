import { Component } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ActivatedRoute, Router } from '@angular/router';
import { IChapterDetailsModel } from '../../viewmodels/chapter.model';

@Component({
  templateUrl: './chapter.component.html'
})
export class ChapterComponent {

  public model: IChapterDetailsModel;
  public selectedExerciseId: number;
  public selectedUserId: number;

  constructor(private chapterService: ChapterService,
    private router: Router,
    private route: ActivatedRoute) {

    this.model = {
      id: 0,
      number: 0,
      exercises: [],
      users: []
    };
    this.selectedExerciseId = 0;
    this.selectedUserId = 0;
  }

  ngOnInit() {

    this.route.parent.params.subscribe(parentParams => {
      let courseId = +parentParams['courseId']; // (+) converts 'courseId' to a number
      this.route.params.subscribe(params => {
        let chapterNumber = +params['chapterNumber']; // (+) converts 'chapterNumber' to a number

        this.chapterService.getChapterDetails(courseId, chapterNumber).subscribe((chapterDetails: IChapterDetailsModel) => {
          this.model = chapterDetails;
          this.selectedExerciseId = 0;
          this.selectedUserId = chapterDetails.users[0].id;
          this.router.navigate(['users', this.selectedUserId], { relativeTo: this.route });
        });

      });
    });
  }

  public onSelectionChanged() {
    if (this.selectedExerciseId > 0) {
      this.router.navigate(['users', this.selectedUserId, 'exercises', this.selectedExerciseId],
        { relativeTo: this.route });
    } else {
      this.router.navigate(['users', this.selectedUserId], { relativeTo: this.route });
    }
  }
}
