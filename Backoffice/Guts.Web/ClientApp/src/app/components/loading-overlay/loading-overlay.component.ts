import { Component, Input } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-loading-overlay",
  template: `
    @if (show) {
      <div class="app-loading-overlay">
        <div
          class="spinner-border text-light"
          role="status"
          aria-label="Loading"
        ></div>
      </div>
    }
  `,
  styles: [
    `
      .app-loading-overlay {
        position: absolute;
        inset: 0;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: rgba(255, 255, 255, 0.4);
        pointer-events: all;
      }
    `,
  ],
})
export class LoadingOverlayComponent {
  @Input() public show = false;
}
