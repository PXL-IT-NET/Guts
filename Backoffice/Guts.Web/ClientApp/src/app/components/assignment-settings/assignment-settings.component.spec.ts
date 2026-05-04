import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ToastrService } from "ngx-toastr";

import { AssignmentSettingsComponent } from "./assignment-settings.component";
import { AssignmentService, TestService } from "src/app/services";

describe("AssignmentSettingsComponent", () => {
  let component: AssignmentSettingsComponent;
  let fixture: ComponentFixture<AssignmentSettingsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AssignmentSettingsComponent],
      providers: [
        { provide: TestService, useValue: {} },
        { provide: AssignmentService, useValue: {} },
        { provide: ToastrService, useValue: {} },
      ],
    });
    fixture = TestBed.createComponent(AssignmentSettingsComponent);
    component = fixture.componentInstance;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
