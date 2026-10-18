import { Component, Input } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-teacher-docs-dotnet",
  templateUrl: "./teacher-docs-dotnet.component.html",
})
export class TeacherDocsDotnetComponent {
  @Input() public frameworkName = "";
  @Input() public badgeClass = "";
  @Input() public leadText = "";
  @Input() public packageName = "";
  @Input() public projectDescription = "";
  @Input() public minimalExampleCode = "";
}

