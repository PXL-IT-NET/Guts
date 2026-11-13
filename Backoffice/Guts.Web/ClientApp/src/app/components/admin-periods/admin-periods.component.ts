import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { finalize } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { PeriodService } from "../../services/period.service";
import { PeriodModel } from "../../viewmodels/period.model";
import { LoadingOverlayComponent } from "../loading-overlay/loading-overlay.component";
import { DangerDeleteModalComponent } from "../danger-delete-modal/danger-delete-modal.component";

@Component({
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LoadingOverlayComponent,
    DangerDeleteModalComponent,
  ],
  selector: "app-admin-periods",
  templateUrl: "./admin-periods.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPeriodsComponent implements OnInit {
  private readonly periodService = inject(PeriodService);
  private readonly toastr = inject(ToastrService);

  public readonly periods = signal<PeriodModel[]>([]);
  public readonly loading = signal<boolean>(false);

  public editingPeriodId = signal<number | null>(null);
  public editingPeriodIsActive = signal<boolean>(true);
  public formDescription = "";
  public formFrom = "";
  public formUntil = "";

  public readonly periodPendingDelete = signal<PeriodModel | null>(null);

  public ngOnInit(): void {
    this.loadPeriods();
  }

  public loadPeriods(): void {
    this.loading.set(true);
    this.periodService
      .getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        if (result.success) {
          this.periods.set(result.value);
        } else {
          this.toastr.error(result.message || "Failed to load periods.", "Error");
        }
      });
  }

  public isPastPeriod(period: PeriodModel): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const until = new Date(period.until);
    return !period.isActive && until < today;
  }

  public startCreate(): void {
    this.editingPeriodId.set(0);
    this.editingPeriodIsActive.set(true);
    this.formDescription = "";
    this.formFrom = "";
    this.formUntil = "";
  }

  public startEdit(period: PeriodModel): void {
    this.editingPeriodId.set(period.id);
    this.editingPeriodIsActive.set(period.isActive);
    this.formDescription = period.description;
    this.formFrom = this.toDateInput(period.from);
    this.formUntil = this.toDateInput(period.until);
  }

  public cancelForm(): void {
    this.editingPeriodId.set(null);
  }

  public savePeriod(): void {
    const id = this.editingPeriodId();
    if (id === null) {
      return;
    }

    const payload = {
      description: this.formDescription,
      from: this.formFrom,
      until: this.formUntil,
    };

    this.loading.set(true);
    if (id === 0) {
      this.periodService
        .createPeriod(payload)
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe((result) => {
          if (result.success) {
            this.editingPeriodId.set(null);
            this.toastr.success("Period created.", "Success");
            this.loadPeriods();
          } else {
            this.toastr.error(result.message || "Could not create period.", "Error");
          }
        });
    } else {
      this.periodService
        .updatePeriod(id, payload)
        .pipe(finalize(() => this.loading.set(false)))
        .subscribe((result) => {
          if (result.success) {
            this.editingPeriodId.set(null);
            this.toastr.success("Period updated.", "Success");
            this.loadPeriods();
          } else {
            this.toastr.error(result.message || "Could not update period.", "Error");
          }
        });
    }
  }

  public requestDelete(period: PeriodModel): void {
    this.periodPendingDelete.set(period);
  }

  public cancelDelete(): void {
    this.periodPendingDelete.set(null);
  }

  public confirmDelete(): void {
    const period = this.periodPendingDelete();
    if (!period) {
      return;
    }

    this.loading.set(true);
    this.periodService
      .deletePeriod(period.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        this.periodPendingDelete.set(null);
        if (result.success) {
          this.toastr.success(`Period '${period.description}' deleted.`, "Success");
          this.loadPeriods();
        } else {
          this.toastr.error(result.message || "Could not delete period.", "Error");
        }
      });
  }

  private toDateInput(date: Date): string {
    const d = new Date(date);
    return d.toISOString().substring(0, 10);
  }
}
