import { Component, input, ChangeDetectionStrategy } from "@angular/core";

@Component({
  standalone: true,
  selector: "app-loading-overlay",
  template: `
    @if (show()) {
      <div class="app-loading-overlay">
        <div
          class="spinner-border spinner-border-sm text-secondary"
          role="status"
          aria-label="Loading"
        ></div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      .app-loading-overlay {
        position: absolute;
        inset: 0;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: rgba(255, 255, 255, 0.5);
        pointer-events: all;
        z-index: 1050;
      }
    `,
  ],
})
export class LoadingOverlayComponent {
  public show = input<boolean>(false);
}
