import { Component } from '@angular/core';
import { ExerciseService } from '../../services/exercise.service';
import { ActivatedRoute } from '@angular/router';
import { IExerciseDetailModel, ExerciseDetailModel } from '../../viewmodels/exercisedetail.model';
import { ChapterContextProvider } from '../../services/chapter.context.provider';

@Component({
  templateUrl: './exercisedetail.component.html'
})
export class ExerciseDetailComponent {
  private exerciseId: number;
  private userId: number;

  public model: ExerciseDetailModel;

  constructor(private route: ActivatedRoute,
    private exerciseService: ExerciseService,
    private chapterContextProvider: ChapterContextProvider) {
    this.model = {
      exerciseId: 0,
      code: '',
      chapterNumber: 0,
      courseName: '',
      courseId: 0,
      testResults: [],
      firstRun: '',
      lastRun: '',
      numberOfRuns: 0,
      sourceCode: ''
    };

    this.exerciseId = 0;
    this.userId = 0;

    this.chapterContextProvider.contextChanged$.subscribe(() => {
        this.loadExercise();
    });
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.exerciseId = +params['exerciseId']; // (+) converts 'exerciseId' to a number
      if (params['userId']) this.userId = +params['userId'];
      this.loadExercise();
    });
  }

  private loadExercise() {
    this.exerciseService.getExerciseDetail(this.exerciseId, this.userId, this.chapterContextProvider.context.statusDate).subscribe((exerciseDetail: IExerciseDetailModel) => {
      this.model = new ExerciseDetailModel(exerciseDetail);
    });
  }
}
