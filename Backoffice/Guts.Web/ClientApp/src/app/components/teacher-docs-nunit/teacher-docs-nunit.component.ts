import { Component } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-teacher-docs-nunit",
  templateUrl: "./teacher-docs-nunit.component.html",
})
export class TeacherDocsNunitComponent {
  public readonly minimalExampleCode = `[ExerciseTestFixture("dotNet1", "chapter1", "exercise3", @"Exercise1.Desktop\\Views\\MainView.xaml;Exercise1.Desktop\\Views\\MainView.xaml.cs")]
[Apartment(ApartmentState.STA)]
public class MainWindowTests
{
    [MonitoredTest]
    public void ShouldBeAtLeast300PixelsWide()
    {
        var mainWindow = new MainWindow();
        Assert.That(mainWindow.Width, Is.GreaterThan(299));
    }

    [MonitoredTest("Should be at least as wide as (custom test name)")]
    [TestCase(300)]
    [TestCase(400)]
    public void ShouldBeAtLeastAsWideAs(double minWidth)
    {
        var mainWindow = new MainWindow();
        Assert.That(mainWindow.Width, Is.GreaterThanOrEqualTo(minWidth));
    }
}`;
}
