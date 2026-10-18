import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { TeacherDocsConceptsComponent } from "./teacher-docs-concepts.component";

describe("TeacherDocsConceptsComponent", () => {
  let fixture: ComponentFixture<TeacherDocsConceptsComponent>;
  let component: TeacherDocsConceptsComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TeacherDocsConceptsComponent],
      providers: [provideRouter([])],
    });
    fixture = TestBed.createComponent(TeacherDocsConceptsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create and render hero section", () => {
    expect(component).toBeTruthy();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain("Monitor your students' progress with GUTS");
    expect(text).toContain("Centralized progress tracking");
  });
});
