# Guts Java

Java clients for Guts.

## Structure

```
.
├── guts-client-core              # Core library for java clients (test engine independent)
├── guts-client-junit-5           # Client for JUnit 5 (jupiter)
└── guts-client-junit-5-example   # Example for JUnit 5 client
```

## Implementing

See the specific folders on how to implement

```
Client Core     => guts-client-core
Test Engine     => guts-client-<test engine name>
Examples        => guts-client-<test engine name>-example
```

## Usage

Build the modules as packages using Maven. Then, add the ```client-core``` and ```client-junit``` JAR files as dependencies (local or remote) in the project that will provide GUTS support. Use annotations in the tests that should be sent to the GUTS platform.

### Annotations

- Class annotations
  - @GutsFixture(_options_)
    - Options:
      - ```courseCode```: Unique identifier to determine the course
      - ```chapterCode```: Identifier for the chapter
      - ```exerciseCode```: Identifier for the exercise
      - ```sourceCodeRelativeFilePaths```: The source java file that is being tested. Code will be sent along with test results.
  - Example: ```@GutsFixture(courseCode = "javaEss", chapterCode = "C1", exerciseCode = "Exercise01", sourceCodeRelativeFilePaths = "Calculator.java")```
- Test annotations
  - Use of ```@DisplayName``` is encouraged. Value will be used in GUTS.
    - e.g. ```@DisplayName("Calculator needs to be able to add numbers")```
  - Use messages in Assert statements to provide information about failed tests
    - e.g. ```assertEquals(7, result, "Calculator.add(5, 2) should return 7");```
## Goals
- JUnit 5 ( Jupiter )
    - Source can be found in the [guts-client-junit-5](guts-client-junit-5) folder
    - Example can be found in the [guts-client-junit-5-example](guts-client-junit-5-example) folder
- JUnit 4
    - *Todo*
- Other java test frameworks
    - *Todo*
