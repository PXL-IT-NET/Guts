import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import {
  DEFAULT_PAGE_SIZE_OPTIONS,
  IPageSizeOption,
  calculateTotalPages,
} from "../../util/paged-result";

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  selector: "app-pagination-footer",
  templateUrl: "./pagination-footer.component.html",
  styleUrl: "./pagination-footer.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginationFooterComponent {
  public readonly totalCount = input.required<number>();
  public readonly currentItemCount = input.required<number>();
  public readonly currentPage = input.required<number>();
  public readonly pageSize = input.required<number | null>();

  public readonly itemLabel = input<string>("items");
  public readonly ariaLabel = input<string>("Pagination");
  public readonly pageSizeOptions = input<ReadonlyArray<IPageSizeOption>>(
    DEFAULT_PAGE_SIZE_OPTIONS,
  );
  public readonly totalLabelTestId = input<string>("pagination-total-label");
  public readonly paginationTestId = input<string>("pagination-nav");
  public readonly pageSizeTestId = input<string>("page-size-select");
  public readonly selectId = input<string>("pageSize");

  public readonly pageChange = output<number>();
  public readonly pageSizeChange = output<number | null>();

  public readonly totalPages = computed(() =>
    calculateTotalPages(this.totalCount(), this.pageSize()),
  );

  public readonly visiblePages = computed<number[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const maxButtons = 7;
    if (total <= maxButtons) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    let start = Math.max(1, current - 3);
    const end = Math.min(total, start + maxButtons - 1);
    start = Math.max(1, end - maxButtons + 1);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  public readonly displayRangeLabel = computed(() => {
    const total = this.totalCount();
    const count = this.currentItemCount();
    const label = this.itemLabel();
    if (total === 0) {
      return `0 ${label}`;
    }
    const size = this.pageSize();
    if (size === null) {
      return `Showing 1-${total} of ${total} ${label}`;
    }
    const start = (this.currentPage() - 1) * size + 1;
    const end = Math.min(total, start + Math.max(0, count - 1));
    return `Showing ${start}-${end} of ${total} ${label}`;
  });

  public goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.currentPage()) {
      return;
    }
    this.pageChange.emit(page);
  }

  public onPageSizeSelected(newSize: number | null): void {
    this.pageSizeChange.emit(newSize);
  }
}
