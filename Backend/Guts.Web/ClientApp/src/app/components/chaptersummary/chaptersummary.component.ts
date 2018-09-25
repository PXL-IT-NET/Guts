import { Component } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ChapterSummaryModel } from '../../viewmodels/chapter.model';
import { ActivatedRoute } from '@angular/router';

@Component({
  templateUrl: './chaptersummary.component.html'
})
export class ChapterSummaryComponent {
  public model: ChapterSummaryModel;
  public courseId: number;

  constructor(private chapterService: ChapterService,
    private route: ActivatedRoute) {
    this.model = {
      id: 0,
      number: 0,
      userExerciseSummaries: [],
      averageExerciseSummaries: []
    };
    this.courseId = 0;
  }

  ngOnInit() {

    this.route.parent.parent.params.subscribe(grandparentParams => {
      this.courseId = +grandparentParams['courseId']; // (+) converts 'courseId' to a number
      this.route.parent.params.subscribe(parentParams => {
        let chapterNumber = +parentParams['chapterNumber']; // (+) converts 'chapterNumber' to a number

        this.route.params.subscribe(params => {
          let userId = +params['userId']; // (+) converts 'userId' to a number

          this.chapterService.getChapterSummary(this.courseId, chapterNumber, userId).subscribe((chapterContents: ChapterSummaryModel) => {
            this.model = chapterContents;
          });
        });
    
      });
    });
  }
}
