import { Component, Input, ChangeDetectionStrategy } from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
  standalone: true,
  selector: "app-teacher-docs-dotnet",
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
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
