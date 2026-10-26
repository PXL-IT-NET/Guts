import { Component, ChangeDetectionStrategy } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-root",
  templateUrl: "./app.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ["./app.component.css"],
})
export class AppComponent {
  title = "app";
}
