Use the NUnit attributes from Guts.Client.NUnit when you want your automated tests to report results to the GUTS backoffice.

Required setup
- Add the Guts.Client.NUnit package to your test project.
- Ensure gutssettings.json is available and contains GUTS apiBaseUrl and webAppBaseUrl.
- If your system under test is a WPF window, add [Apartment(ApartmentState.STA)] on the fixture.

Class-level attributes
- [ExerciseTestFixture(courseCode, chapterCode, exerciseCode, optionalSourcePaths)]
  Use this for tests of individual exercises within a chapter.
- [ProjectComponentTestFixture(courseCode, projectCode, componentCode, optionalSourcePaths)]
  Use this for tests of project components in team-based project work.

Exercise vs project-component tests in GUTS
- Exercises belong to chapters and are made and evaluated individually.
- Project components belong to projects and are made and evaluated in teams.
- Use [ExerciseTestFixture(...)] for chapter/exercise scenarios.
- Use [ProjectComponentTestFixture(...)] for project/component scenarios.

Parameters of [ExerciseTestFixture]
- courseCode: the code of the course in the GUTS system (provided by the GUTS administrator).
- chapterCode: the code of the chapter. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, a chapter with this code is created if it does not exist yet.
- exerciseCode: the code of the exercise. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, an exercise with this code is created in the chapter if it does not exist yet.
- optionalSourcePaths: optional semicolon-separated paths to the source files relevant for the test. These paths are relative to the solution directory. The contents of these files are sent to the GUTS backoffice.

Parameters of [ProjectComponentTestFixture]
- courseCode: the code of the course in the GUTS system (provided by the GUTS administrator).
- projectCode: the code of the project. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, a project with this code is created if it does not exist yet.
- componentCode: the code of the component. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, a component with this code is created if it does not exist yet.
- optionalSourcePaths: optional semicolon-separated paths to the source files relevant for the test. These paths are relative to the solution directory. The contents of these files are sent to the GUTS backoffice.

Method-level attribute
- [MonitoredTest] replaces [Test] for monitored runs. The name of the test method will be used as the test name in the GUTS backoffice. The camelcased method name will be split into separate words. For example, "ShouldBeAtLeast300PixelsWide" will be reported as "Should be at least 300 pixels wide".
- [MonitoredTest("Custom test name")] sets a custom report name. Most of the time, the default name (based on the method name) will be sufficient. This is actually a deprecated overload.
- [MonitoredTest] can be used with [TestCase] for data-driven test methods.

How collection and submission works
- The fixture attribute initializes configuration and result sender dependencies.
- Each monitored test result is captured.
- Results are accumulated per test class.
- At class completion, the test results, hash of the test-code and contents of source files are sent to the GUTS backoffice.
- Results are only sent if all the tests of a fixture are run. If you run only a subset of the tests, no results will be sent to the GUTS backoffice.
- Results will only be accepted by the GUTS backoffice if the test-code hash matches one of the hashed registerd when the test class was run by a lector. This means that if a student changes the test code, the results will be rejected by the GUTS backoffice. This is to prevent tampering with the tests after a run has been made.

Minimal example
```csharp
[ExerciseTestFixture("dotNet1", "chapter1", "exercise3", @"Exercise1.Desktop\Views\MainView.xaml;Exercise1.Desktop\Views\MainView.xaml.cs")]
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
}