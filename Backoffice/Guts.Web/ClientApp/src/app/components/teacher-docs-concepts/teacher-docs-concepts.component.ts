import { Component, ChangeDetectionStrategy } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";

@Component({
  standalone: true,
  selector: "app-teacher-docs-concepts",
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./teacher-docs-concepts.component.html",
})
export class TeacherDocsConceptsComponent {}
