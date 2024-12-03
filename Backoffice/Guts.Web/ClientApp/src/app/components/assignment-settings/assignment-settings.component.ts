import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { AssignmentService, TestService } from 'src/app/services';
import { PostResult } from 'src/app/util/result';
import { IAssignmentModel, ITestModel } from 'src/app/viewmodels/assignment.model';

@Component({
  selector: 'app-assignment-settings',
  templateUrl: './assignment-settings.component.html'
})
export class AssignmentSettingsComponent {
  public loading: boolean;

  @Input() public assignment: IAssignmentModel;
  @Input() public assignmentKind: string;
  @Output() public assignmentChanged = new EventEmitter<IAssignmentModel>();
 
  constructor(
    private testService: TestService,
    private assignmentService: AssignmentService,
    private toastr: ToastrService
  ) {
    this.loading = false;
    this.assignment = {} as IAssignmentModel;
    this.assignmentKind = "assignment";
  }

  ngOnInit() {
    
  }
  public deleteAssignment(assignment: IAssignmentModel) {
    let confirmMessage = "Are you sure you want to remove '" + assignment.description + "'? All tests and test results will also be deleted.";
    if (confirm(confirmMessage)) {
      this.loading = true;
      this.assignmentService.deleteAssignment(assignment.assignmentId)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.toastr.info("The " + this.assignmentKind + " '" + assignment.description + "' is succesfully deleted", this.capitalizeFirstLetter(this.assignmentKind) + " deleted");
            this.assignmentChanged.emit(this.assignment);
          } else {
            this.toastr.error(result.message || "unknown error", "Could not delete " + this.assignmentKind);
          }
        });
    }
  }

  public deleteTest(assignment: IAssignmentModel, test: ITestModel) {
    let confirmMessage = "Are you sure you want to remove the test '" + test.testName + "' from the " + this.assignmentKind + " '" + assignment.description + "'? The linked test results will also be deleted.";
    if (confirm(confirmMessage)) {
      this.loading = true;
      this.testService.deleteTest(test.id)
        .subscribe((result: PostResult) => {
          this.loading = false;
          if (result.success) {
            this.toastr.info("The test '" + test.testName + "' is succesfully deleted", "Test deleted");
            this.assignmentChanged.emit(this.assignment);
          } else {
            this.toastr.error(result.message || "unknown error", "Could not delete test");
          }
        });
    }
  }

  private capitalizeFirstLetter(input: string): string {
    if (!input) return input;
    return input.charAt(0).toUpperCase() + input.slice(1);
  }
}
