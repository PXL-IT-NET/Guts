import { Component } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ChapterContentsModel } from '../../viewmodels/chapter.model';
import { ActivatedRoute } from '@angular/router';

@Component({
    templateUrl: './chaptercontents.component.html'
})
export class ChapterContentsComponent {
    public model: ChapterContentsModel;
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
        this.route.params.subscribe(params => {
            this.courseId = +params['courseId']; // (+) converts 'courseId' to a number
            let chapterNumber = +params['chapterNumber']; // (+) converts 'chapterNumber' to a number

            this.chapterService.getChapterSummary(this.courseId, chapterNumber).subscribe((chapterContents: ChapterContentsModel) => {
                this.model = chapterContents;
            });
        });
    }
}
