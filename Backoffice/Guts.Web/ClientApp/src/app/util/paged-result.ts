import { HttpParams } from "@angular/common/http";

export interface IPagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number | null;
}

export interface IPageSizeOption {
  label: string;
  value: number | null;
}

export const DEFAULT_PAGE_SIZE_OPTIONS: ReadonlyArray<IPageSizeOption> = [
  { label: "25", value: 25 },
  { label: "50", value: 50 },
  { label: "100", value: 100 },
  { label: "All", value: null },
];

export function calculateTotalPages(
  totalCount: number,
  pageSize: number | null,
): number {
  if (pageSize === null || pageSize <= 0) {
    return 1;
  }
  return Math.max(1, Math.ceil(totalCount / pageSize));
}

export function createPagedSearchParams(
  search: string,
  page: number,
  pageSize: number | null,
): HttpParams {
  let params = new HttpParams().set("page", page.toString());
  if (search.trim()) {
    params = params.set("search", search.trim());
  }
  if (pageSize !== null) {
    params = params.set("pageSize", pageSize.toString());
  }
  return params;
}
