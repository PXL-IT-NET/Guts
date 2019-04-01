package be.pxl.guts.core.models;

import java.util.List;

public class AssignmentTestRun {

    private Assignment assignment;
    private List<TestResult> results;
    private String sourceCode;
    private String testCodeHash;

    public AssignmentTestRun(Assignment assignment, List<TestResult> results, String sourceCode, String testCodeHash) {
        this.assignment = assignment;
        this.results = results;
        this.sourceCode = sourceCode;
        this.testCodeHash = testCodeHash;
    }

    public Assignment getAssignment() {
        return assignment;
    }

    public List<TestResult> getResults() {
        return results;
    }

    public String getSourceCode() {
        return sourceCode;
    }

    public void setSourceCode(String sourceCode) {
        this.sourceCode = sourceCode;
    }

    public String getTestCodeHash() {
        return testCodeHash;
    }

    public void setTestCodeHash(String testCodeHash) {
        this.testCodeHash = testCodeHash;
    }

}
