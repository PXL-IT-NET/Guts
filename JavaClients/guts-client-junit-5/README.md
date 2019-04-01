# Guts JUnit 5 client

This library uses the JUnit 5 [TestExecutionListener](https://junit.org/junit5/docs/5.2.0/api/org/junit/platform/launcher/TestExecutionListener.html) for getting the test results to send to the Guts Rest Api.

## Installation

### Maven
To add guts support to maven projects, add the following dependencies to pom.xml
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

## Build to local maven
To build the project to the local maven repository run the command below in the project folder.
```
gradle publishToMavenLocal
```

## Supported test cases
- @Test 
- @RepeatedTest 
- @ParameterizedTest 
    - @EnumSource 
    - @ArgumentsSource 
    - @MethodSource 
    - @ValueSource 
    - @CsvSource 
    - @CsvFileSource 

## Example tests

Please refer to the [guts-client-junit-5-example](../guts-client-junit-5-example) folder for an example on implementing in tests