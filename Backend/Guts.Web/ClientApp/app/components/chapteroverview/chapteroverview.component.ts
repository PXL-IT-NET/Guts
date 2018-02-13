import { Component } from '@angular/core';
import { ChapterService } from '../../services/chapter.service';
import { ChapterContentsModel } from '../../viewmodels/chaptercontents.model';
import { ExerciseSummaryModel } from '../../viewmodels/exercisesummary.model';

@Component({
    templateUrl: './chapteroverview.component.html'
})
export class ChapterOverviewComponent {
    public model: ChapterContentsModel;
    public loading: boolean;

    constructor(private chapterService: ChapterService) {
        this.model = {
            exercises: []
        };
        this.loading = false;

        chapterService.getChapterSummary(1, 5).subscribe((chapterContents: ChapterContentsModel) => {
            this.model = chapterContents;
        });
    }
}
