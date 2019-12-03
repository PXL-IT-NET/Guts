import { Component, OnInit, OnDestroy  } from '@angular/core';
import { ExamService } from '../../services/exam.service';
import { AuthService } from "../../services/auth.service";
import { ActivatedRoute } from '@angular/router';
import { UserProfile } from "../../viewmodels/user.model";
import { ExamModel, ExamPartModel } from "../../viewmodels/exam.model";
import { Subscription } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

@Component({
  templateUrl: './courseconfig.component.html'
})
export class CourseConfigComponent implements OnInit, OnDestroy  {
  public loading: boolean = false;
  public userProfile: UserProfile;
  public exams: ExamModel[];
  public newExam: ExamModel;

  private userProfileSubscription: Subscription;
  private courseId: number;

  constructor(private route: ActivatedRoute,
    private examService: ExamService,
    private authService: AuthService,
    private toastr: ToastrService) {

    this.exams = [];
    this.newExam = new ExamModel();
    this.courseId = 0;
  }

  ngOnInit() {
    this.userProfile = new UserProfile();
    this.userProfileSubscription = this.authService.getUserProfile().subscribe(profile => {
      this.userProfile = profile;
    });

    this.route.params.subscribe(params => {
      this.courseId = +params['courseId']; // (+) converts 'courseId' to a number

      this.loading = true;
      this.examService.getExamsOfCourse(this.courseId).subscribe(result => {
        this.loading = false;
        if (result.success) {
          this.exams = result.value;
        } else {
          this.toastr.error("Could not retrieve exams from API. Message: " + (result.message || "unknown error"), "API error");
        }
      });
     
    });
  }

  ngOnDestroy() {
    this.userProfileSubscription.unsubscribe();
  }

  public saveNewExam() {
    this.newExam.courseId = this.courseId;
    this.examService.saveExam(this.newExam).subscribe(result => {
      this.loading = false;
      if (result.success) {
        this.exams.push(result.value);
        this.newExam = new ExamModel();
      } else {
        this.toastr.error("Could not save exam. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }

  public onExamPartDeleted(examPart: ExamPartModel, exam: ExamModel){
    exam.parts.splice(exam.parts.indexOf(examPart), 1)
  }

  public onExamPartAdded(examPart: ExamPartModel, exam: ExamModel){
    exam.parts.push(examPart);
  }
}
