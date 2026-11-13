import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { LoadingOverlayComponent } from "../loading-overlay/loading-overlay.component";

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingOverlayComponent],
  selector: "app-danger-delete-modal",
  templateUrl: "./danger-delete-modal.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DangerDeleteModalComponent {
  public readonly show = input<boolean>(false);
  public readonly loading = input<boolean>(false);
  public readonly title = input.required<string>();
  public readonly warningMessage = input.required<string>();
  public readonly expectedConfirmation = input.required<string>();
  public readonly confirmationLabel = input.required<string>();
  public readonly confirmButtonText = input<string>("Confirm Delete");

  public readonly confirmed = output<void>();
  public readonly cancelled = output<void>();

  public readonly confirmationInput = signal<string>("");
  public readonly canConfirm = computed(
    () =>
      this.expectedConfirmation().length > 0 &&
      this.confirmationInput() === this.expectedConfirmation(),
  );

  constructor() {
    effect(() => {
      if (!this.show()) {
        this.confirmationInput.set("");
      }
    });
  }

  public onConfirm(): void {
    if (!this.canConfirm()) {
      return;
    }
    this.confirmationInput.set("");
    this.confirmed.emit();
  }

  public onCancel(): void {
    this.confirmationInput.set("");
    this.cancelled.emit();
  }
}
