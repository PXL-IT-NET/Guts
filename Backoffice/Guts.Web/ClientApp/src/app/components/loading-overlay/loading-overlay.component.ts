import { Component, Input } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-loading-overlay",
  template: `
    <div class="app-loading-overlay" *ngIf="show">
      <div
        class="spinner-border text-light"
        role="status"
        aria-label="Loading"
      ></div>
    </div>
  `,
  styles: [
    `
      .app-loading-overlay {
        position: fixed;
        inset: 0;
        z-index: 2000;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: rgba(255, 255, 255, 0.3);
      }
    `,
  ],
})
export class LoadingOverlayComponent {
  @Input() public show = false;
}
