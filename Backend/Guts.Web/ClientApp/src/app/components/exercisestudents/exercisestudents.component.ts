import { Component } from '@angular/core';
import { ExerciseService } from '../../services/exercise.service';
import { ActivatedRoute } from '@angular/router';
import { IUserModel } from "../../viewmodels/user.model";

@Component({
  templateUrl: './exercisestudents.component.html'
})
export class ExerciseStudentsComponent {
  public model: any;

  constructor(private route: ActivatedRoute,
    private exerciseService: ExerciseService) {
    this.model = {
      exerciseId: 0,
      users: []
    };
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      let exerciseId = +params['exerciseId']; // (+) converts 'exerciseId' to a number
      this.model.exerciseId = exerciseId;
      this.exerciseService.getExerciseStudents(exerciseId).subscribe((users: IUserModel[]) => {
        this.model.users = users;
      });
    });
  }
}
