import { ComponentFixture, TestBed } from "@angular/core/testing";
import { DangerDeleteModalComponent } from "./danger-delete-modal.component";

describe("DangerDeleteModalComponent", () => {
  let component: DangerDeleteModalComponent;
  let fixture: ComponentFixture<DangerDeleteModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DangerDeleteModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DangerDeleteModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput("show", true);
    fixture.componentRef.setInput("title", "Danger: Delete Course");
    fixture.componentRef.setInput("warningMessage", "Irreversible!");
    fixture.componentRef.setInput("expectedConfirmation", "NET1");
    fixture.componentRef.setInput("confirmationLabel", "course code");
    fixture.detectChanges();
  });

  it("should disable confirm button until exact confirmation text is entered", () => {
    const confirmBtn = fixture.nativeElement.querySelector(
      "button.btn-danger",
    ) as HTMLButtonElement;
    expect(confirmBtn.disabled).toBeTrue();

    component.confirmationInput.set("NET");
    fixture.detectChanges();
    expect(confirmBtn.disabled).toBeTrue();

    component.confirmationInput.set("NET1");
    fixture.detectChanges();
    expect(confirmBtn.disabled).toBeFalse();
  });

  it("should emit confirmed when confirm button is clicked with matching text", () => {
    spyOn(component.confirmed, "emit");
    component.confirmationInput.set("NET1");
    fixture.detectChanges();

    const confirmBtn = fixture.nativeElement.querySelector(
      "button.btn-danger",
    ) as HTMLButtonElement;
    confirmBtn.click();

    expect(component.confirmed.emit).toHaveBeenCalled();
  });

  it("should emit cancelled and clear input when cancel is clicked", () => {
    spyOn(component.cancelled, "emit");
    component.confirmationInput.set("NET1");
    component.onCancel();

    expect(component.cancelled.emit).toHaveBeenCalled();
    expect(component.confirmationInput()).toBe("");
  });

  it("should show extraWarning and allow immediate confirm when requireConfirmationInput is false", () => {
    fixture.componentRef.setInput("requireConfirmationInput", false);
    fixture.componentRef.setInput(
      "extraWarning",
      "Warning: This user is a TEACHER!",
    );
    fixture.componentRef.setInput(
      "extraWarningTestId",
      "teacher-delete-warning",
    );
    fixture.detectChanges();

    const warning = fixture.nativeElement.querySelector(
      "[data-testid='teacher-delete-warning']",
    );
    expect(warning).toBeTruthy();
    expect(warning.textContent).toContain("TEACHER");
    expect(
      fixture.nativeElement.querySelector("[data-testid='danger-confirm-input']"),
    ).toBeNull();

    const confirmBtn = fixture.nativeElement.querySelector(
      "button.btn-danger",
    ) as HTMLButtonElement;
    expect(confirmBtn.disabled).toBeFalse();
  });
});
