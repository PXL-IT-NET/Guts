# Guts .NET client
The .NET client is a set of Nuget packages that 
- contain extensions on NUnit to send results of test to the Guts Rest API.
- contain components that can help teachers to write automated tests for exercises.

A teacher creates a C# solution that contains one or more projects, one for each exercise (e.g. a WPF project named *Exercise01*).
For each exercise project the teacher adds a test project that will hold the automated tests for the exercise (e.g. a class library named *Exercise01.Tests*).

There are 2 packages available:
* Guts.Client.Core -> package that can be used for creating automated tests in .NET test projects targeting .NET Core 3.1 or higher.
* Guts.Client.WPF -> the package that can be used when creating automated tests on a WPF application (.net6).

## Installation
### Add NuGet packages
Add the *Guts.Client.Core* package to your test project.
```
Install-Package Guts.Client.Core
```

Optionally, when writing tests for a WPF project, add the *Guts.Client.WPF* package to your test project.
```
Install-Package Guts.Client.WPF
```
The WPF package contains some helper classes and extension methods to test a _WPF Window_.


## Writing a first test
Let's assume we are writing a test for a WPF Exercise that has *MainWindow* as its start window.

Create a test class (e.g. *MainWindowTests*).

```csharp
[ExerciseTestFixture("dotNet1", "chapter1", "exercise3", @"Views\MainView.xaml;Views\MainView.xaml.cs")]
[Apartment(ApartmentState.STA)]
public class MainWindowTests
{
    private MainWindow _mainWindow;

    [SetUp]
    public void Setup()
    {
        _mainWindow = new MainWindow();
    }

    [TearDown]
    public void TearDown()
    {
        _mainWindow?.Dispose();
    }

    [MonitoredTest]
    public void ShouldBeAtLeast300PixelsWide()
    {
        Assert.That(_mainWindow.Width, Is.GreatherThan(299));
    }
}
```

You should use *NUnit* as your test framework (*NUnit* is included in the *Guts.Client* package).

Instead of the `[TestFixture]` attribute you must use the `[ExerciseTestFixture]` attribute.
This attribute takes the following parameters:
1. The code of the course in the backend.
2. The code of the chapter in the backend.
3. The code of the exercise in the backend.

So the test above is a test for exercise 3 of chapter 1 of a course with code *dotNet1*.

Instead of the `[Test]` attribute you must use the `[MonitoredTest]` attribute.

By using these attributes, test results of a testrun will be collected and sent to the Guts Rest API.

Also note the `[Apartment(ApartmentState.STA)]` attribute. This is needed when the system under test (sut) is a WPF window. For another (normal) sut, this attribute is not needed.

## Running / adding tests
When running monitored tests a browser window will open on the login page of the GUTS system. Provide your credentials to receive a key to the GUTS api (handling of the key happens automagically). No login is needed until the key is expired.

What happens next depends on who is executing the tests.
* If a teacher runs the tests, the chapter, exercise and tests are created in the GUTS system if they did not exist yet. Also a hash of the testcode file is registerd in the GUTS system as being a valid hash for those tests.
* If a student runs the test, the test results are send to the GUTS system. If the hash of the testcode is valid (the student has not changed testcode), then the GUTS system accepts and registers the results.

When something goes wrong during this process inspect the test ouput (*Output Window -> Show output from tests*). This output will show what went wrong.

## Tools
The *Guts.Client.Core* nuget package contains some helper classes and extensions to make it easier to write tests:
- Solution class
- CodeCleaner class
- Object extensions

The *Guts.Client.WPF* nuget package contains some extra helper classes and extensions to make it easier to write tests for WPF applications:
- WPF tools
  - Dependency object extensions
  - Button extensions
  - ...

### Solution class and CodeCleaner class
You can use the `Solution` class to retrieve the contents of a code file.
You can use the `CodeCleaner` class to remove comments from source code.

```csharp
[MonitoredTest]
public void ShouldMakeUseOfThePolygonclass()
{
    var sourceCode = Solution.Current.GetFileContent(@"Exercise05\MainWindow.xaml.cs");
    sourceCode = CodeCleaner.StripComments(sourceCode);

    Assert.That(sourceCode, Contains.Substring("new Polygon();"), 
        () => "No code found where an instance of the Polygon class is created.");
}
```

### Object extensions
You can use the `ObjectExtensions` check if an object (e.g. MainWindow) has a certain private field.
You can also use it to retrieve the value of that field.

### WPF

#### Dependency object extensions
If you want check if a visual element contains other visual elements of a certain type, you can use the `FindVisualChildren<T>()` extension method.

```csharp
[MonitoredTest]
public void ShouldHaveAGenderGroupBox()
{
    Assert.That(_genderGroupBox, Is.Not.Null, () => "Could not find a GroupBox control with header 'Geslacht'");

    var radioButtons = _genderGroupBox.FindVisualChildren<RadioButton>().ToList();
    
    Assert.That(radioButtons.Count, Is.EqualTo(2), () => "Could not find 2 RadioButtons within the gender groupbox");
}
```

#### Button extensions
Use the `FireClickEvent()` extension method to simulate a click on a button.

```csharp
[MonitoredTest]
public void ShouldCalculateBtwAtRate21WhenCheckBoxIsUnchecked()
{
    AssertAllControlsArePresent();

    var netPrice = "50";
    _priceTextBox.Text = netPrice;
    _checkBox.IsChecked = false;

    _button.FireClickEvent();

    Assert.That(_btwTextBox.Text, Is.EqualTo("10,5"), () => $"Btw for net price of '{netPrice}' is not correct");
    Assert.That(_totalTextBox.Text, Is.EqualTo("60,5"), () => $"Total for net price of '{netPrice}' is not correct");
}
```

