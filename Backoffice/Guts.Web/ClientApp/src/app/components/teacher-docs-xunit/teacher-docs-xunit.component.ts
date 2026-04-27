import { Component } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-teacher-docs-xunit",
  templateUrl: "./teacher-docs-xunit.component.html",
})
export class TeacherDocsXunitComponent {
  public readonly minimalExampleCode = `[ExerciseTestClass("dotNet1", "chapter1", "exercise3", @"Exercise1.Desktop\\Views\\MainView.xaml;Exercise1.Desktop\\Views\\MainView.xaml.cs")]
[Apartment(ApartmentState.STA)]
  public class MainWindowTests
{
    [MonitoredFact]
    public void ShouldBeAtLeast300PixelsWide()
    {
        var mainWindow = new MainWindow();
        Assert.True(mainWindow.Width > 299);
    }

    [MonitoredTheory]
    [InlineData(300)]
    [InlineData(400)]
    public void ShouldBeAtLeastAsWideAs(double minWidth)
    {
        var mainWindow = new MainWindow();
        Assert.True(mainWindow.Width >= minWidth);
    }
}`;
}
