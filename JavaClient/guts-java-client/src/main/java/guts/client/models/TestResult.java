package guts.client.models;

public class TestResult {

    private String testName;
    private boolean passed;
    private String message;

    public TestResult(String testName, boolean passed, String message) {
        this.testName = testName;
        this.passed = passed;
        this.message = message;
    }

    public String getTestName() {
        return testName;
    }
}
