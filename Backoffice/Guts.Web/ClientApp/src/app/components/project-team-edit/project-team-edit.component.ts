import { Component, Input } from '@angular/core';
import { ITeamDetailsModel, ITeamMemberModel } from '../../viewmodels/team.model';
import { BsModalRef } from 'ngx-bootstrap/modal';
import { FormGroup, FormControl, Validators, AbstractControl } from '@angular/forms';
import { ProjectService } from '../../services';
import { PostResult } from '../../util/result';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-project-team-edit',
  templateUrl: './project-team-edit.component.html'
})
export class ProjectTeamEditComponent {

  @Input() public courseId: number;
  @Input() public projectCode: string;
  @Input() public team: ITeamDetailsModel;

  public loading: boolean;
  public editTeamForm: FormGroup;

  constructor(
    public modalRef: BsModalRef,
    private projectService: ProjectService,
    private toastr: ToastrService) {

    this.loading = false;
  }

  ngOnInit() {
    this.editTeamForm = new FormGroup({
      "name": new FormControl(null, Validators.required)
    });

    this.editTeamForm.patchValue(this.team);
  }

  onSubmit() {
    if (this.editTeamForm.invalid) return;

    //copy form values to team model
    Object.assign(this.team, this.editTeamForm.getRawValue());

    this.loading = true;
    this.projectService.updateTeam(this.courseId, this.projectCode, this.team).subscribe(
      (result: PostResult) => {
        this.loading = false;
        if (result.success) {
          this.modalRef.hide();
        } else {
          this.toastr.error(result.message || "unknown error", "Could not update team");
        }
      }
    );
  }

  public removeFromTeam(member: ITeamMemberModel) {
    let confirmMessage = "Are you sure you want to remove '" + member.name + "' from '" + this.team.name + "'? All test results and peer assessments of this team will also be deleted.";
    if (confirm(confirmMessage)) {
      this.loading = true;
      this.projectService.removeFromTeam(this.courseId, this.projectCode, this.team.id, member.userId)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            //remove member from team
            this.team.members = this.team.members.filter(m => m.userId !== member.userId);
          } else {
            this.toastr.error(result.message || "unknown error", "Could not remove user from team");
          }
        });
    }
  }

  public isInvalid(formControl: AbstractControl): boolean {
    return formControl.invalid && (formControl.dirty || formControl.touched);
  }
}
