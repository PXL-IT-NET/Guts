package be.pxl.guts.core.models;

import java.util.List;

public class AssignmentTestRun {

    private Assignment assignment;
    private List<TestResult> results;
    private List<SolutionFile> solutionFiles;
    private String testCodeHash;

    public AssignmentTestRun(Assignment assignment, List<TestResult> results, List<SolutionFile> solutionFiles, String testCodeHash) {
        this.assignment = assignment;
        this.results = results;
        this.solutionFiles = solutionFiles;
        this.testCodeHash = testCodeHash;
    }

    public Assignment getAssignment() {
        return assignment;
    }

    public List<TestResult> getResults() {
        return results;
    }

    public List<SolutionFile> getSolutionFiles() {
        return solutionFiles;
    }

    public void setSolutionFiles(List<SolutionFile> solutionFiles) {
        this.solutionFiles = solutionFiles;
    }

    public String getTestCodeHash() {
        return testCodeHash;
    }

    public void setTestCodeHash(String testCodeHash) {
        this.testCodeHash = testCodeHash;
    }

}
