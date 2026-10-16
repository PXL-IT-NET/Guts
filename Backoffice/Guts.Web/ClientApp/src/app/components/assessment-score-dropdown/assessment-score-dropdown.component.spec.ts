import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormsModule } from "@angular/forms";
import { AssessmentScoreDropdownComponent } from "./assessment-score-dropdown.component";

describe("AssessmentScoreDropdownComponent", () => {
  let component: AssessmentScoreDropdownComponent;
  let fixture: ComponentFixture<AssessmentScoreDropdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AssessmentScoreDropdownComponent],
      imports: [FormsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(AssessmentScoreDropdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should initialize disabled=false and emit valueChange when onValueChange is called", () => {
    expect(component.disabled).toBeFalse();

    let emittedValue: number;
    component.valueChange.subscribe((v) => (emittedValue = v));

    component.onValueChange(4);

    expect(component.value).toBe(4);
    expect(emittedValue).toBe(4);
  });
});
