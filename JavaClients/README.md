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

## Goals
- JUnit 5 ( Jupiter )
    - Source can be found in the [guts-client-junit-5](guts-client-junit-5) folder
    - Example can be found in the [guts-client-junit-5-example](guts-client-junit-5-example) folder
- JUnit 4
    - *Todo*
- Other java test frameworks
    - *Todo*
