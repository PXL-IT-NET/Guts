import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { ProjectService, ProjectTeamAssessmentService } from 'src/app/services';
import { ProjectAssessmentService } from 'src/app/services/project.assessment.service';
import { IProjectDetailsModel } from 'src/app/viewmodels/project.model';
import { IProjectAssessmentModel, ProjectAssessmentCreateModel, ProjectAssessmentModel } from 'src/app/viewmodels/projectassessment.model';

@Component({
  selector: 'app-project-assessment-overview',
  templateUrl: './project-assessment-overview.component.html'
})
export class ProjectAssessmentOverviewComponent implements OnInit {

  public project: IProjectDetailsModel;
  public assessments: ProjectAssessmentModel[];
  public selectedTeamId: number;

  //#Form
  public assessmentForm: FormGroup;
  
  public get descriptionControl() { return <FormControl>this.assessmentForm.get('description'); }
  public get openOnControl() { return <FormControl>this.assessmentForm.get('openOn'); }
  public get deadlineControl() { return <FormControl>this.assessmentForm.get('deadline'); }
  //#End Form


  constructor(private formBuilder: FormBuilder,
    private projectService: ProjectService,
    private projectAssessmentService: ProjectAssessmentService,
    private projectTeamAssessmentService: ProjectTeamAssessmentService,
    private toastr: ToastrService,
    private route: ActivatedRoute) {
    this.project = {
      id: 0,
      code: '',
      description: '',
      components: [],
      teams: []
    };

    this.assessments = [];
    this.selectedTeamId = 0;
  }

  ngOnInit(): void {
    //#Form
    this.assessmentForm = this.formBuilder.group({
      'description': [null, [Validators.required]],
      'openOn': [null, [Validators.required]],
      'deadline': [null, [Validators.required]]
    });
    //#End Form

    let courseId = +this.route.parent.snapshot.params['courseId']
    let projectCode = this.route.snapshot.params['code'];
    this.projectService.getProjectDetails(courseId, projectCode).subscribe(result => {
      if (result.success) {
        this.project = result.value;
        if(this.project.teams.length > 0){
          this.selectedTeamId = this.project.teams[0].id;
        } 
        this.loadProjectAssessments(); 
      } else {
        this.toastr.error("Could not load project from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });

  }

  public onAssessmentSubmit() {
    this.assessmentForm.markAllAsTouched();
    if (this.assessmentForm.invalid) return;
    if (this.project == null) return;

    let model = new ProjectAssessmentCreateModel(this.project.id);
    Object.assign(model, this.assessmentForm.getRawValue());

    this.projectAssessmentService.addProjectAssessment(model).subscribe(result =>{
      if (result.success) {
        this.toastr.success("Project peer assessment added");
        this.assessments.push(result.value);
      } else {
        this.toastr.error("Could not add project peer assessment. Message: " + (result.message || "unknown error"), "API error");
      }
    })
  }

  public canShowErrors(fc: FormControl): boolean {
    return fc.dirty || fc.touched
  }

  public loadProjectAssessments(){
    this.projectAssessmentService.getAssessmentsOfProject(this.project.id).subscribe(result => {
      if (result.success) {
        this.assessments = result.value;
        //this.assessments = this.assessments.slice(0,1);
        this.assessments.forEach(assessment => {
          if(assessment.isOpen){
            this.projectTeamAssessmentService.getStatusOfProjectTeamAssessment(assessment.id, this.selectedTeamId).subscribe(result => {
              if (result.success) {
                 assessment.teamStatus = result.value;
              } else {
                this.toastr.warning("Could not retrieve project team assment status. Message: " + (result.message || "unknown error"), "API error");
              }
            });
          }
        });

      } else {
        this.toastr.error("Could not load project assessments from API. Message: " + (result.message || "unknown error"), "API error");
      }
    });
  }

}
