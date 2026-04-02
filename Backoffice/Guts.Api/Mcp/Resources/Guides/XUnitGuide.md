Use the xUnit attributes from Guts.Client.XUnit when you want your automated tests to report results to the GUTS backoffice.

Required setup
- Add the Guts.Client.XUnit package to your test project.
- Ensure gutssettings.json is available and contains GUTS apiBaseUrl and webAppBaseUrl.

Class-level attributes
- [ExerciseTestClass(courseCode, chapterCode, exerciseCode, optionalSourcePaths)]
  Use this for tests of individual exercises within a chapter.
- [ProjectComponentTestClass(courseCode, projectCode, componentCode, optionalSourcePaths)]
  Use this for tests of project components in team-based project work.

Exercise vs project-component tests in GUTS
- Exercises belong to chapters and are made individually.
- Project components belong to projects and are made in teams.
- Use [ExerciseTestClass(...)] for chapter/exercise scenarios.
- Use [ProjectComponentTestClass(...)] for project/component scenarios.

Parameters of [ExerciseTestClass]
- courseCode: the code of the course in the GUTS system (provided by the GUTS administrator).
- chapterCode: the code of the chapter. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, a chapter with this code is created if it does not exist yet.
- exerciseCode: the code of the exercise. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, an exercise with this code is created in the chapter if it does not exist yet.
- optionalSourcePaths: optional semicolon-separated paths to the source files relevant for the test. These paths are relative to the solution directory. The contents of these files are sent to the GUTS backoffice.

Note: all code parameters should be lowercase and contain no spaces. Use dashes or underscores if you want to separate words. The maximum length of a code is 20 characters.

Parameters of [ProjectComponentTestClass]
- courseCode: the code of the course in the GUTS system (provided by the GUTS administrator).
- projectCode: the code of the project. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, a project with this code is created if it does not exist yet.
- componentCode: the code of the component. You can choose this code. If a lector runs the tests and results are sent to https://guts-api.pxl.be/, a component with this code is created if it does not exist yet.
- optionalSourcePaths: optional semicolon-separated paths to the source files relevant for the test. These paths are relative to the solution directory. The contents of these files are sent to the GUTS backoffice.

Note: all code parameters should be lowercase and contain no spaces. Use dashes or underscores if you want to separate words. The maximum length of a code is 20 characters.

Method-level attributes
- [MonitoredFact] replaces [Fact] for monitored single-case tests. The name of the test method will be used as the test name in the GUTS backoffice. The camelcased method name will be split into separate words. For example, "ShouldBeAtLeast300PixelsWide" will be reported as "Should be at least 300 pixels wide".
- [MonitoredFact("Custom test name")] sets a custom report name. Most of the time, the default name (based on the method name) will be sufficient.
- [MonitoredTheory] replaces [Theory] for monitored parameterized tests.
- Combine [MonitoredTheory] with [InlineData] for data-driven test cases.

How collection and submission works
- The class attribute initializes configuration and result sender dependencies.
- Each monitored fact or theory result is captured.
- Results are accumulated per test class.
- At class completion, the test results, hash of the test-code and contents of source files are sent to the GUTS backoffice.
- Results are only sent if all the tests of a class are run. If you run only a subset of the tests, no results will be sent to the GUTS backoffice.
- Results will only be accepted by the GUTS backoffice if the test-code hash matches one of the hashes registered when the test class was run by a lector. This means that if a student changes the test code, the results will be rejected by the GUTS backoffice. This is to prevent tampering with the tests after a run has been made.

Minimal example
```csharp
[ExerciseTestClass("dotNet1", "chapter1", "exercise3", @"Exercise1.Desktop\Views\MainView.xaml;Exercise1.Desktop\Views\MainView.xaml.cs")]
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
}