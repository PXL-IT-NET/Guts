package guts.client.models;

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

}
