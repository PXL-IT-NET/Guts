import { ComponentFixture, TestBed } from "@angular/core/testing";
import { LoadingOverlayComponent } from "./loading-overlay.component";

describe("LoadingOverlayComponent", () => {
  let fixture: ComponentFixture<LoadingOverlayComponent>;
  let component: LoadingOverlayComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LoadingOverlayComponent],
    });

    fixture = TestBed.createComponent(LoadingOverlayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create and not display overlay by default", () => {
    expect(component.show()).toBeFalse();
    const overlay = fixture.nativeElement.querySelector(".app-loading-overlay");
    expect(overlay).toBeNull();
  });

  it("should display overlay when show input signal is set to true", () => {
    fixture.componentRef.setInput("show", true);
    fixture.detectChanges();

    expect(component.show()).toBeTrue();
    const overlay = fixture.nativeElement.querySelector(".app-loading-overlay");
    expect(overlay).not.toBeNull();
    const spinner = fixture.nativeElement.querySelector(".spinner-border");
    expect(spinner).not.toBeNull();
  });
});
