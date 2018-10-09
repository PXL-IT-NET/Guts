import { Component, OnInit } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ChapterContextProvider } from '../../services/chapter.context.provider';
import { ChapterSummaryModel } from '../../viewmodels/chapter.model';
import { ActivatedRoute } from '@angular/router';

@Component({
  templateUrl: './chaptersummary.component.html'
})
export class ChapterSummaryComponent implements OnInit{
  public model: ChapterSummaryModel;
  private courseId: number;
  private chapterNumber: number;
  private userId: number;

  constructor(private chapterService: ChapterService,
    private chapterContextProvider: ChapterContextProvider,
    private route: ActivatedRoute) {
    this.model = {
      id: 0,
      number: 0,
      userExerciseSummaries: [],
      averageExerciseSummaries: []
    };
    this.courseId = 0;
    this.chapterNumber = 0;
    this.userId = 0;

    this.chapterContextProvider.contextChanged$.subscribe(() => {
        this.loadChapterSummary();
    });
  }

  ngOnInit() {
    this.route.parent.parent.params.subscribe(grandparentParams => {
      this.courseId = +grandparentParams['courseId']; // (+) converts 'courseId' to a number
      this.route.parent.params.subscribe(parentParams => {
        this.chapterNumber = +parentParams['chapterNumber']; // (+) converts 'chapterNumber' to a number
        this.route.params.subscribe(params => {
          this.userId = +params['userId']; // (+) converts 'userId' to a number
          this.loadChapterSummary();
        }); 
      });
    });
  }

  private loadChapterSummary() {
    this.chapterService.getChapterSummary(this.courseId, this.chapterNumber, this.userId, this.chapterContextProvider.context.statusDate).subscribe((chapterContents: ChapterSummaryModel) => {
      this.model = chapterContents;
    });
  }
}
