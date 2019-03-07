# Guts Java client

The java client is a package that contains an extension on JUnit 5 jupiter to send results of tests to the Guts Rest Api.

## Installation

### Maven
To add guts support to maven projects, add the following dependencies to pom.xml
```
<dependency>
    <groupId>pxl.guts</groupId>
    <artifactId>guts-client-java</artifactId>
    <version>1.0-SNAPSHOT</version>
    <scope>test</scope>
</dependency>
```

### Gradle

To add guts support to gradle projects, add the following dependencies to the build.gradle file.
```
dependencies {
    testImplementation 'pxl.guts:guts-client-java:1.0-SNAPSHOT'
}
```

### Configuration

When tests are run for the first time a file *guts.json* will be added to the project root folder. This file contains the projects Guts configuration.

```
baseUrl = the url of the Guts Rest API
registerUrl = the url of the register button on the login form
sourceDirectory = path to the source files from the root folder
testDirectory = path to the test files from the root folder

root folder = folder that contains the *guts.json* file.
```

The default configuration file is configured for maven and gradle projects
```
{  
   "baseUrl":"http://localhost:54830/",
   "registerUrl":"http://localhost:54831/register",
   "sourceDirectory":"src/main/java/",
   "testDirectory":"src/test/java/"
}
```

## Build to local maven
To build the project to the local maven repository run the command below in the project folder.
```
gradle publishToMavenLocal
```

## Example tests

Calculator.java class

```
public class Calculator {

    public class Calculator {

    public int add(int one, int two) {
        return one + two;
    }

    public int sub(int one, int two) {
        return one - two;
    }

}
```

CalculatorTest.java class
```
import guts.client.annotations.GutsFixture;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

import static org.junit.jupiter.api.Assertions.assertEquals;

@GutsFixture(courseCode = "java1", chapterCode = "H01", exerciseCode = "Exercises01", sourceCodeRelativeFilePaths = "test/Calculator.java")
class CalculatorTest {
    @Test
    @DisplayName("Calculator needs to be able to add numbers")
    public void add() {
        Calculator calc = new Calculator();
        int result = calc.add(5, 2);

        assertEquals(7, result);
    }

    @Test
    @DisplayName("Calculator needs to be able to subtract numbers")
    public void sub() {
        Calculator calc = new Calculator();
        int result = calc.sub(5, 3);

        assertEquals(2, result, "The result of sub(5, 3) should be 2 but is " + result);
    }
    
}
```

Parameterized tests are supported
```
@ParameterizedTest(name = "{displayName} [{index}] {arguments}")
@DisplayName("Parameterized Test")
@ValueSource(strings = { "param1", "param2", "param3"})
public void parameterizedTest(String param) {
    assertNotNull(param);
}
```

Repeated tests are also supported
```
@RepeatedTest(value = 5, name = "{displayName} {currentRepetition}/{totalRepetitions}")
@DisplayName("Repeating Test")
public void repeatedTest(RepetitionInfo repInfo, TestInfo testInfo) {
    System.out.println(testInfo.getDisplayName() + "-->" + repInfo.getCurrentRepetition());
    boolean greater = repInfo.getCurrentRepetition() >= 2;
    assertTrue(greater, "Repetition should be greater or equal to 2");
}
```