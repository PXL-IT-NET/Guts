# Guts JUnit 5 client

This library uses the JUnit 5 [TestExecutionListener](https://junit.org/junit5/docs/5.2.0/api/org/junit/platform/launcher/TestExecutionListener.html) for getting the test results to send to the Guts Rest Api.

## Using

To use the Guts JUnit 5 client you have to use maven or gradle to implement the dependency

### Maven
```
<dependency>
    <groupId>be.pxl.guts</groupId>
    <artifactId>client-junit-5</artifactId>
    <version>1.0-SNAPSHOT</version>
    <scope>test</scope>
</dependency>
```

### Gradle

To add guts support to gradle projects, add the following dependencies to the build.gradle file.
```gradle
dependencies {
    testImplementation 'be.pxl.guts:client-junit-5:1.0-SNAPSHOT'
}
```

### Configuration

When tests are run for the first time a file *guts.json* will be added to the project root folder. This file contains the projects Guts configuration.

```
baseUrl = the url of the Guts Rest API
webUrl = the url of the register button on the login form
sourceDirectory = path to the source files from the root folder
testDirectory = path to the test files from the root folder

root folder = folder that contains the *guts.json* file.
```

The default configuration file is configured for maven and gradle projects
```json
{  
   "baseUrl":"http://localhost:54830/",
   "webUrl":"http://localhost:54831/",
   "sourceDirectory":"src/main/java/",
   "testDirectory":"src/test/java/"
}
```

## Example Tests

#### Example class to test
```java

public class Calculator {

    public int add(int one, int two) {
        return one + two;
    }

    public int sub(int one, int two) {
        return one - two;
    }

}

```

#### Example test class

```java

@GutsFixture(courseCode = "java1", chapterCode = "H01", exerciseCode = "Exercises01", sourceCodeRelativeFilePaths = "Calculator.java")
class CalculatorTest {

    @Test
    @DisplayName("Calculator needs to be able to add numbers")
    void shouldAddTwoNumbers() {
        Calculator calc = new Calculator();
        int result = calc.add(5, 2);

        assertEquals(7, result, "Calculator.add(5, 2) should return 7");
    }

    @Test
    @DisplayName("Calculator needs to be able to subtract numbers")
    void shouldSubtractTwoNumbers() {
        Calculator calc = new Calculator();
        int result = calc.sub(5, 3);

        assertEquals(2, result, "The result of sub(5, 3) should be 2 but is " + result);
    }

}

```

### Other supported types

#### Repeated tests
```java

@RepeatedTest(value = 5, name = "{displayName} {currentRepetition}/{totalRepetitions}")
@DisplayName("Repeating Test")
public void repeatedTest(RepetitionInfo repInfo, TestInfo testInfo) {
    assertTrue(true, "Should be true");
}

```

#### Parameterized tests
```java

// ---- EnumSource
@ParameterizedTest(name = "{displayName} [{index}] {arguments}")
@EnumSource(value = TestEnum.class, names = "Some.+", mode = EnumSource.Mode.MATCH_ALL)
public void EnumTest(TestEnum value) {
    assertTrue(true, "Should be true");
}

public enum TestEnum {
    Some,
    SomeOther,
    SomeThing,
    ThingSomee
}

// ---- ArgumentsSource (class)
@ParameterizedTest(name = "{displayName} [{index}] {arguments}")
@DisplayName("Parameterized addition test")
@ArgumentsSource(NumberArgumentsProvider.class)
public void additionParameterizedTestClass(int num1, int num2, int expected) {
    Calculator calc = new Calculator();
    int result = calc.add(num1, num2);

    assertEquals(expected, result, "Calculator.add(" + num1 + ", " + num2 +") should return " + expected);
}

static class NumberArgumentsProvider implements ArgumentsProvider {
    @Override
    public Stream<? extends Arguments> provideArguments(ExtensionContext context) throws Exception {
        return Stream.of(
                Arguments.of(2, 3, 5),
                Arguments.of(6, 5, 11),
                Arguments.of(7, 8, 15),
                Arguments.of(2000, 6000, 8000),
                Arguments.of(52551, 2568, 55119)
        );
    }
}

// ---- MethodSource
@ParameterizedTest(name = "{displayName} [{index}] {arguments}")
@MethodSource("provideStringsForIsBlank")
void isBlank_ShouldReturnTrueForNullOrBlankStrings(String input, boolean expected) {
    assertEquals(expected, input == null || input.trim().equals(""));
}

private static Stream<Arguments> provideStringsForIsBlank() {
    return Stream.of(
            Arguments.of(null, true),
            Arguments.of("", true),
            Arguments.of("  ", true),
            Arguments.of("not blank", false)
    );
}

// ---- ValueSource
@ParameterizedTest(name = "{displayName} [{index}] {arguments}")
@ValueSource(ints= {1, 3, 5, 15})
public void shouldReturnTrueForOddNumbers(int number) {
    assertTrue(number % 2 == 1);
}

// ---- CsvSource
@ParameterizedTest(name = "{displayName} [{index}] {arguments}")
@CsvSource({"test,TEST", "tEst,TEST", "Java,JAVA"})
void toUpperCase_ShouldGenerateTheExpectedUppercaseValue(String input, String expected) {
    String actualValue = input.toUpperCase();
    assertEquals(expected, actualValue);
}

// ---- CsvFileSource
@ParameterizedTest(name = "{displayName} [{index}] {arguments}")
@CsvFileSource(resources = "/data.csv", numLinesToSkip = 1)
void toUpperCase_ShouldGenerateTheExpectedUppercaseValueCSVFile(String input, String expected) {
    String actualValue = input.toUpperCase();
    assertEquals(expected, actualValue);
}

```
