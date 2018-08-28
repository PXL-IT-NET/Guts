import { Component } from '@angular/core';
import { ExerciseService } from '../../services/exercise.service';
import { ActivatedRoute } from '@angular/router';
import { IExerciseDetailModel } from '../../viewmodels/exercisedetail.model';

@Component({
  templateUrl: './exercisedetail.component.html'
})
export class ExerciseDetailComponent {
  public model: IExerciseDetailModel;

  constructor(private route: ActivatedRoute,
    private exerciseService: ExerciseService) {
    this.model = {
      exerciseId: 0,
      number: 0,
      chapterNumber: 0,
      courseName: '',
      courseId: 0,
      testResults: [],
      firstRun: '',
      lastRun: '',
      numberOfRuns: 0
    };
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      let exerciseId = +params['exerciseId']; // (+) converts 'exerciseId' to a number
      this.exerciseService.getExerciseDetail(exerciseId).subscribe((exerciseDetail: IExerciseDetailModel) => {
        this.model = exerciseDetail;
      });
    });
  }
}
