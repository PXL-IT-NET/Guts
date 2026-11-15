import { ComponentFixture, TestBed } from "@angular/core/testing";
import { PaginationFooterComponent } from "./pagination-footer.component";

describe("PaginationFooterComponent", () => {
  let component: PaginationFooterComponent;
  let fixture: ComponentFixture<PaginationFooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PaginationFooterComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PaginationFooterComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput("totalCount", 60);
    fixture.componentRef.setInput("currentItemCount", 25);
    fixture.componentRef.setInput("currentPage", 1);
    fixture.componentRef.setInput("pageSize", 25);
    fixture.componentRef.setInput("itemLabel", "users");
    fixture.detectChanges();
  });

  it("should render display range, centered pages, and items per page options", () => {
    expect(component.totalPages()).toBe(3);
    expect(component.visiblePages()).toEqual([1, 2, 3]);
    expect(component.displayRangeLabel()).toBe("Showing 1-25 of 60 users");
    expect(
      fixture.nativeElement.querySelector("ul.justify-content-center"),
    ).toBeTruthy();
  });

  it("should emit pageChange and pageSizeChange events", () => {
    const pageSpy = spyOn(component.pageChange, "emit");
    const sizeSpy = spyOn(component.pageSizeChange, "emit");

    component.goToPage(2);
    expect(pageSpy).toHaveBeenCalledWith(2);

    component.goToPage(1);
    component.goToPage(99);
    expect(pageSpy).toHaveBeenCalledTimes(1);

    component.onPageSizeSelected(null);
    expect(sizeSpy).toHaveBeenCalledWith(null);
  });

  it("should show all items range when pageSize is null", () => {
    fixture.componentRef.setInput("totalCount", 12);
    fixture.componentRef.setInput("currentItemCount", 12);
    fixture.componentRef.setInput("pageSize", null);
    fixture.detectChanges();

    expect(component.totalPages()).toBe(1);
    expect(component.displayRangeLabel()).toBe("Showing 1-12 of 12 users");
  });
});
