import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";
import { ToastrService } from "ngx-toastr";
import { AdminPeriodsComponent } from "./admin-periods.component";
import { PeriodService } from "../../services/period.service";
import { PeriodModel } from "../../viewmodels/period.model";
import { GetResult, PostResult } from "../../util/result";
import { createToastrSpy } from "../../testing/test-helpers";

describe("AdminPeriodsComponent", () => {
  let component: AdminPeriodsComponent;
  let fixture: ComponentFixture<AdminPeriodsComponent>;
  let periodServiceSpy: jasmine.SpyObj<PeriodService>;
  let toastrSpy: jasmine.SpyObj<ToastrService>;

  beforeEach(async () => {
    periodServiceSpy = jasmine.createSpyObj("PeriodService", [
      "getAll",
      "createPeriod",
      "updatePeriod",
      "deletePeriod",
    ]);
    const pastPeriod = new PeriodModel({
      id: 1,
      description: "2020-2021",
      from: new Date("2020-09-01"),
      until: new Date("2021-06-30"),
      isActive: false,
    });
    periodServiceSpy.getAll.and.returnValue(
      of(GetResult.success([pastPeriod])),
    );
    periodServiceSpy.deletePeriod.and.returnValue(of(PostResult.success()));
    toastrSpy = createToastrSpy();

    await TestBed.configureTestingModule({
      imports: [AdminPeriodsComponent],
      providers: [
        { provide: PeriodService, useValue: periodServiceSpy },
        { provide: ToastrService, useValue: toastrSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminPeriodsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should load periods on init and identify past periods", () => {
    expect(component.periods()).toHaveSize(1);
    expect(component.isPastPeriod(component.periods()[0])).toBeTrue();
  });

  it("should not call createPeriod when dates are empty", () => {
    component.startCreate();
    component.formDescription = "2026-2027";
    component.formFrom = "";
    component.formUntil = "";

    component.savePeriod();

    expect(periodServiceSpy.createPeriod).not.toHaveBeenCalled();
    expect(toastrSpy.error).toHaveBeenCalled();
  });

  it("should allow deleting future non-active periods while disabling delete for active periods", () => {
    const activePeriod = new PeriodModel({
      id: 2,
      description: "Active",
      from: new Date(Date.now() - 86400000),
      until: new Date(Date.now() + 86400000),
      isActive: true,
    });
    const futurePeriod = new PeriodModel({
      id: 3,
      description: "Future",
      from: new Date(Date.now() + 86400000 * 30),
      until: new Date(Date.now() + 86400000 * 60),
      isActive: false,
    });
    component.periods.set([activePeriod, futurePeriod]);
    fixture.detectChanges();

    const deleteButtons = fixture.nativeElement.querySelectorAll(
      "tbody tr button.btn-outline-danger",
    ) as NodeListOf<HTMLButtonElement>;
    expect(deleteButtons).toHaveSize(2);
    expect(deleteButtons[0].disabled).toBeTrue();
    expect(deleteButtons[1].disabled).toBeFalse();
  });

  it("should handle load error and cancel/guard no-ops", () => {
    periodServiceSpy.getAll.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Load failed" } as GetResult<PeriodModel[]>),
    );
    component.loadPeriods();
    expect(toastrSpy.error).toHaveBeenCalledWith("Load failed", "Error");

    component.savePeriod();
    expect(periodServiceSpy.createPeriod).not.toHaveBeenCalled();

    component.startCreate();
    component.cancelForm();
    expect(component.editingPeriodId()).toBeNull();

    component.confirmDelete();
    expect(periodServiceSpy.deletePeriod).not.toHaveBeenCalled();
  });

  it("should create, edit, update, and delete periods on success and error", () => {
    const existing = component.periods()[0];
    periodServiceSpy.createPeriod.and.returnValue(of(GetResult.success(existing)));
    component.startCreate();
    component.formDescription = "2026-2027";
    component.formFrom = "2026-09-01";
    component.formUntil = "2027-06-30";
    component.savePeriod();
    expect(toastrSpy.success).toHaveBeenCalledWith("Period created.", "Success");

    periodServiceSpy.createPeriod.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Create err" } as GetResult<PeriodModel>),
    );
    component.startCreate();
    component.formDescription = "2026-2027";
    component.formFrom = "2026-09-01";
    component.formUntil = "2027-06-30";
    component.savePeriod();
    expect(toastrSpy.error).toHaveBeenCalledWith("Create err", "Error");

    periodServiceSpy.updatePeriod.and.returnValue(of(PostResult.success()));
    component.startEdit(existing);
    expect(component.formFrom).toBe("2020-09-01");
    component.savePeriod();
    expect(toastrSpy.success).toHaveBeenCalledWith("Period updated.", "Success");

    periodServiceSpy.updatePeriod.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Update err" }),
    );
    component.startEdit(existing);
    component.savePeriod();
    expect(toastrSpy.error).toHaveBeenCalledWith("Update err", "Error");

    component.requestDelete(existing);
    component.cancelDelete();
    expect(component.periodPendingDelete()).toBeNull();

    component.requestDelete(existing);
    component.confirmDelete();
    expect(toastrSpy.success).toHaveBeenCalledWith(
      "Period '2020-2021' deleted.",
      "Success",
    );

    periodServiceSpy.deletePeriod.and.returnValue(
      of({ success: false, isAuthenticated: true, message: "Delete err" }),
    );
    component.requestDelete(existing);
    component.confirmDelete();
    expect(toastrSpy.error).toHaveBeenCalledWith("Delete err", "Error");
  });
});
