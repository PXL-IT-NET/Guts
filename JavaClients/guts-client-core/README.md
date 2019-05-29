# Guts Core Client Library

This library contains all the test engine independent features to integrate Guts. These are used to easily implement new test engines.

## Installation

### Maven
To add guts support to maven projects, add the following dependencies to pom.xml
```
<dependency>
    <groupId>be.pxl.guts</groupId>
    <artifactId>client-core</artifactId>
    <version>1.0-SNAPSHOT</version>
    <scope>test</scope>
</dependency>
```

### Gradle

To add guts support to gradle projects, add the following dependencies to the build.gradle file.
```gradle
dependencies {
    testImplementation 'be.pxl.guts:client-core:1.0-SNAPSHOT'
}
```


## Build to local maven
To build the project to the local maven repository run the command below in the project folder.
```
gradle publishToMavenLocal
```

## Features
- Guts integration ✔
- Utility classes ( Todo )
    - Reflection
        - Check if private fields and methods exist
        - Get value of private fields

## Usage

To use the guts client core library for integrating Guts in a test engine. You will need to use the GutsCore and TestAccumulator classes.

#### GutsCore

```java
// Default options are BaseConfig and SwingAuthorizationHandler
GutsCore gutsCore = new GutsCore(IConfig config, IAuthorizationHandler authorizationHandler);

// Get the Guts logger
gutsCore.getLogger(); 

// Get the Guts config
gutsCore.getConfig(); 

// Send the test results to the Guts Api
gutsCore.sendResults(TestAccumulator testAccumulator); 
```

#### TestAccumulator

```java
// courseCode   => The Guts identifier for the course
// chapterCode  => The Guts identifier for the chapter/topic
// exerciseCode => The Guts identifier for the exercise/assignment
// testRunType  => The TestRuntype (FOR_EXERCISE / FOR_PROJECT) that indicates with endpoint should be used.
// sourceCode   => The source code of the file that are tested (Can Be Null)
// hash         => The hash of the test file
TestAccumulator testAccumulator = new TestAccumulator(String courseCode, String chapterCode, String exerciseCode, TestRunType testRunType, String sourceCode, String hash);

// Add test results to the accumulator
testAccumulator.addTestResult(String testName, boolean successful, String message);
testAccumulator.addTestResult(TestResult testResult);
```