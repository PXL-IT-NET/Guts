import { Component } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ChapterContentsModel } from '../../models/chaptercontents.model';
import { ExerciseSummaryModel } from '../../models/exercisesummary.model';

@Component({
    templateUrl: './chapteroverview.component.html'
})
export class ChapterOverviewComponent {
    public model: any;
    public loading: boolean;
    public result: string;

    constructor(private chapterService: ChapterService) {
        this.model = {};
        this.loading = false;
        this.result = '';

        chapterService.getChapterSummary(1, 5).subscribe((chapterContents: ChapterContentsModel) => {
            for (let exercise of chapterContents.exercises) {
                this.result += exercise.number + ', ';
            }
        });
    }
}
