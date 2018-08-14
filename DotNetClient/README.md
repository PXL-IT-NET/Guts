# Guts .NET client
The .NET client is a set of Nuget packages that 
- contain extensions on NUnit to send results of test to the Guts Rest API.
- contain components that can help teachers to write automated tests for exercises.

A teacher creates a C# solution that contains one or more projects, one for each exercise (e.g. a WPF project named *Exercise01*).
For each exercise project the teacher adds a test project that will hold the automated tests for the exercise (e.g. a class library named *Exercise01.Tests*).

There are 3 packages available:
* Guts.Client.Shared -> a .NET standard library that contains shared tools and extensions that can be used in the classic .NET framework but also in the .NET Core framework. This package is the common stuff used by the two other packages.
* Guts.Client -> the package that can be used when creating automated tests in the .NET Classic framework.
* Guts.Client.Core -> the package that can be used when creating automated tests in the .NET Core framework.

## Installation
### .NET Classic
When working in the classic .NET Framework, add the *Guts.Client* package to your test project.
```
Install-Package Guts.Client
```
An *app.config* file will be added to the test project and it should contain an app setting *GutsApiUrl* which holds the url of the Rest API to which test results are sent.
If the app setting is missing, you shoud add it to the config file.

```xml
<appSettings>
    <add key="GutsApiUri" value="https://guts-api.appspot.com/" />
</appSettings>
```
### .NET Core
When working in the .NET Core Framework, add the *Guts.Client.Core* package to your test project.
```
Install-Package Guts.Client.Core
```

## Writing a first test
### .NET Classic
Let's assume we are writing a test for a WPF Exercise that has *MainWindow* as its start window.

Create a test class (e.g. *MainWindowTests*).

```csharp
[MonitoredTestFixture("dotNet1", 5, 3), Apartment(ApartmentState.STA)]
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

    [MonitoredTest("Should be at least 300 pixels wide")]
    public void ShouldBeAtLeast300PixelsWide()
    {
        Assert.That(_mainWindow.Width, Is.GreatherThan(299));
    }
}
```

You should use *NUnit* as your test framework (*NUnit* is included in the *Guts.Client* package).

Instead of the `[TestFixture]` attribute you must use the `[MonitoredTestFixture]` attribute.
This attribute takes 3 parameters:
1. The code of the course in the backend.
2. The number of the chapter.
3. The number of the exercise.

So the test above is a test for exercise 3 of chapter 5 of a course with code *dotNet1*.

Instead of the `[Test]` attribute you must use the `[MonitoredTest]` attribute.
This attribute takes expects a (human readable) description of the test.

By using these attributes, test results of a testrun will be collected and sent to the Guts Rest API.

Also note the `[Apartment(ApartmentState.STA)]` attribute. This is needed because a login screen is shown when the students do their first test run.
Without this attribute, no test results will be sent to the Guts REST Api.

### .NET Core
The way of working in .NET Core is similar, but now there is no need to add the `[Apartment(ApartmentState.STA)]` attribute.
This is because the login screen that is shown when the students do their first test run, is opened in a browser.

## Tools
The *Guts.Client.Shared* nuget package contains some helper classes and extensions to make it easier to write tests:
- Solution class
- CodeCleaner class
- Object extensions

The *Guts.Client* nuget package contains some extra helper classes and extensions to make it easier to write tests for WPF applications:
- WPF tools
  - TestWindow class
  - Dependency object extensions
  - Button extensions

### Solution class and CodeCleaner class
You can use the `Solution` class to retrieve the contents of a code file.
You can use the `CodeCleaner` class to remove comments from source code.

```csharp
[MonitoredTest("Should make use of the Polygon class")]
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

Available extension methods:
- HasPrivateField
- HasPrivateFieldValue
- GetPrivateFieldValue
- GetPrivateFieldValueByName
- GetAllPrivateFieldValues

```csharp
[OneTimeSetUp]
public void Setup()
{
    _testWindow = new TestWindow<MainWindow>();
    _dispatcherTimer = _testWindow.GetPrivateField<DispatcherTimer>();
    if (_dispatcherTimer != null)
    {
        _tickEventHandler = _dispatcherTimer.GetPrivateFieldValueByName<EventHandler>(nameof(DispatcherTimer.Tick));
    }
}
```

### WPF

#### TestWindow class
Wrap a `Window` into a `TestWindow` class to get easy access to the UIElements in the window and to the private fields of the window.

```csharp
[OneTimeSetUp]
public void Setup()
{
    _testWindow = new TestWindow<MainWindow>();
    _hasNameLabel = _testWindow.GetContentControlByPartialContentText<Label>("naam") != null;
    _hasNameTextBox = _testWindow.GetPrivateField<TextBox>() != null;
    _okButton = _testWindow.GetContentControlByPartialContentText<Button>("ok");
    _progressBar = _testWindow.GetUIElements<ProgressBar>().FirstOrDefault();
}
```

#### Dependency object extensions
If you want check if a visual element contains other visual elements of a certain type, you can use the `FindVisualChildren<T>()` extension method.

```csharp
[MonitoredTest("Should have a gender groupbox")]
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
[MonitoredTest("Should calculate Btw at 21% when checkbox is unchecked")]
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

