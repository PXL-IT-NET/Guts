package be.pxl.guts.core.util;

import be.pxl.guts.core.enums.TestRunType;
import be.pxl.guts.core.models.Assignment;
import be.pxl.guts.core.models.AssignmentTestRun;
import be.pxl.guts.core.models.SolutionFile;
import be.pxl.guts.core.models.TestResult;

import java.util.ArrayList;
import java.util.List;

/**
 * Helper class for collecting test results
 */
public class TestAccumulator {

    private Assignment assignment;
    private TestRunType testRunType;
    private List<SolutionFile> solutionFiles;
    private String hash;

    private List<TestResult> testResults;

    /**
     * Default constructor
     *
     * @param courseCode identifier code for the course
     * @param chapterCode identifier code for the chapter
     * @param exerciseCode identifier code for the exercise
     * @param testRunType type of test run
     * @param solutionFiles list of SolutionFile objects that were used
     * @param hash hash of the test file
     */
    public TestAccumulator(String courseCode, String chapterCode, String exerciseCode, TestRunType testRunType, List<SolutionFile> solutionFiles, String hash) {
        this.assignment = new Assignment(courseCode, chapterCode, exerciseCode);
        this.testRunType = testRunType;
        this.solutionFiles = solutionFiles;
        this.hash = hash;

        this.testResults = new ArrayList<>();
    }

    /**
     * Add a test result to the collector
     *
     * @param testResult result to add to the collector
     */
    public void addTestResult(TestResult testResult) {
        testResults.add(testResult);
    }

    /**
     * Add a test result to the collector
     *
     * @param testName name the of the test to add to the collector
     * @param successful whether the test was successful or not
     * @param message error message of the test
     */
    public void addTestResult(String testName, boolean successful, String message) {
        testResults.add(new TestResult(testName, successful, message));
    }

    /**
     * Convert {@link TestAccumulator} to {@link AssignmentTestRun} object
     *
     * @return {@link AssignmentTestRun}
     */
    public AssignmentTestRun toAssignmentTestRun() {
        return new AssignmentTestRun(this.assignment, this.testResults, this.solutionFiles, this.hash);
    }

    /**
     * Get the type of test run
     * @return {@link TestRunType} of current fixture
     */
    public TestRunType getTestRunType() {
        return testRunType;
    }

}
