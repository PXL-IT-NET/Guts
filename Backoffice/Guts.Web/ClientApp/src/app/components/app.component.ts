import { Component, ChangeDetectionStrategy } from "@angular/core";
import { Chart } from "chart.js";

@Component({
  standalone: false,
  selector: "app-root",
  templateUrl: "./app.component.html",
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ["./app.component.css"],
})
export class AppComponent {
  title = "app";
}
