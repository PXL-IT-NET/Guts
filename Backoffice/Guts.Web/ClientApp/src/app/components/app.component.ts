import { Component, ChangeDetectionStrategy, inject } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../services/auth.service";

@Component({
  standalone: false,
  selector: "app-root",
  templateUrl: "./app.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrls: ["./app.component.css"],
})
export class AppComponent {
  title = "app";
  public readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  public stopImpersonating(): void {
    this.authService.stopImpersonation();
    void this.router.navigate(["/home"]);
  }
}
