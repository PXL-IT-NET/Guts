import { ComponentFixture, TestBed } from "@angular/core/testing";
import { TeacherDocsJunitComponent } from "./teacher-docs-junit.component";

describe("TeacherDocsJunitComponent", () => {
  let fixture: ComponentFixture<TeacherDocsJunitComponent>;
  let component: TeacherDocsJunitComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TeacherDocsJunitComponent],
    });
    fixture = TestBed.createComponent(TeacherDocsJunitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create and render JUnit documentation", () => {
    expect(component).toBeTruthy();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain("Write monitored tests with JUnit 5");
    expect(text).toContain("client-junit-5");
    expect(text).toContain("@GutsFixture");
  });
});

