import { CommonModule } from "@angular/common";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { TeacherDocsNunitComponent } from "../teacher-docs-nunit/teacher-docs-nunit.component";
import { TeacherDocsXunitComponent } from "../teacher-docs-xunit/teacher-docs-xunit.component";
import { TeacherDocsDotnetComponent } from "./teacher-docs-dotnet.component";

describe("TeacherDocsDotnetComponent & .NET Teacher Docs", () => {
  let dotnetFixture: ComponentFixture<TeacherDocsDotnetComponent>;
  let dotnetComponent: TeacherDocsDotnetComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CommonModule],
      declarations: [
        TeacherDocsDotnetComponent,
        TeacherDocsNunitComponent,
        TeacherDocsXunitComponent,
      ],
    });
    dotnetFixture = TestBed.createComponent(TeacherDocsDotnetComponent);
    dotnetComponent = dotnetFixture.componentInstance;
  });

  it("should render shared .NET documentation fields", () => {
    dotnetComponent.frameworkName = "NUnit";
    dotnetComponent.badgeClass = "bg-success";
    dotnetComponent.leadText = "Lead text";
    dotnetComponent.packageName = "Guts.Client.NUnit";
    dotnetComponent.projectDescription = "test project";
    dotnetComponent.minimalExampleCode = "public class SampleTests {}";
    dotnetFixture.detectChanges();

    const text = dotnetFixture.nativeElement.textContent as string;
    expect(text).toContain("Write monitored tests with NUnit");
    expect(text).toContain("Guts.Client.NUnit");
    expect(text).toContain("public class SampleTests {}");
  });

  it("should render TeacherDocsNunitComponent and TeacherDocsXunitComponent using shared component", () => {
    const nunitFixture = TestBed.createComponent(TeacherDocsNunitComponent);
    nunitFixture.detectChanges();
    expect(nunitFixture.nativeElement.textContent).toContain("[MonitoredTest]");

    const xunitFixture = TestBed.createComponent(TeacherDocsXunitComponent);
    xunitFixture.detectChanges();
    expect(xunitFixture.nativeElement.textContent).toContain("[MonitoredFact]");
  });
});

