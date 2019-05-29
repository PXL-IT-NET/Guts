package be.pxl.guts.core.models;

public class TestResult {

    private String testName;
    private boolean passed;
    private String message;

    public TestResult(String testName, boolean successful, String message) {
        this.testName = testName;
        this.passed = successful;
        this.message = message;
    }

    public String getTestName() {
        return testName;
    }

    public boolean isPassed() {
        return passed;
    }

    public String getMessage() {
        return message;
    }
}
