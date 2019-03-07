package guts.client;

import guts.client.annotations.GutsFixture;
import guts.client.http.GutsHttpClient;
import guts.client.http.TestResultSender;
import guts.client.models.Assignment;
import guts.client.models.AssignmentTestRun;
import guts.client.models.Result;
import guts.client.models.TestResult;
import guts.client.utility.FileUtil;
import guts.client.utility.GutsTestUtil;
import guts.client.utility.TestRunResultAccumulator;
import org.junit.platform.engine.TestExecutionResult;
import org.junit.platform.engine.support.descriptor.ClassSource;
import org.junit.platform.launcher.TestExecutionListener;
import org.junit.platform.launcher.TestIdentifier;
import org.junit.platform.launcher.TestPlan;

import java.util.logging.ConsoleHandler;
import java.util.logging.LogRecord;
import java.util.logging.Logger;
import java.util.logging.SimpleFormatter;

/**
 * The main class for Guts Client for Java
 *
 * Implementation of {@link TestExecutionListener}
 *
 * @author Jeroen Verwimp
 */
public class GutsJUnit5 implements TestExecutionListener {

    private static Logger logger;
    private static TestPlan testPlan;
    private TestResultSender testResultSender;

    public GutsJUnit5() {
        Configuration.setup();
    }

    @Override
    public void testPlanExecutionStarted(TestPlan testPlan) {
        this.testPlan = testPlan;
    }

    @Override
    public void executionFinished(TestIdentifier testIdentifier, TestExecutionResult testExecutionResult) {
        if(!testIdentifier.isTest())
            return;

        ClassSource classSource = GutsTestUtil.getGutsClassSource(testIdentifier);
        if(classSource == null) // Not a guts test
            return;

        GutsFixture fixture = GutsTestUtil.getGutsFixture(classSource);

        // load data to test run result
        TestRunResultAccumulator.getInstance().setupMetaData(classSource);

        String testName = testIdentifier.getDisplayName();
        boolean passed = testExecutionResult.getStatus() == TestExecutionResult.Status.SUCCESSFUL;
        String throwableMessage = testExecutionResult.getThrowable().isPresent() ? testExecutionResult.getThrowable().get().toString() : "";

        TestResult testResult = new TestResult(testName, passed, throwableMessage);
        TestRunResultAccumulator.getInstance().addTestResult(testResult);

        if(TestRunResultAccumulator.getInstance().allTestOfFixtureWereRunned()) {
            Assignment assignment = new Assignment(fixture);
            String sourceCode = FileUtil.getSourceCode(fixture.sourceCodeRelativeFilePaths());
            AssignmentTestRun testRun = new AssignmentTestRun(assignment, TestRunResultAccumulator.getInstance().getTestResults(), sourceCode, TestRunResultAccumulator.getInstance().getTestCodeHash());

            Result result = getTestResultSender().send(testRun, fixture.testRunType());
            if(result.isSuccessful()) {
                getLogger().info("Successfully uploaded the results of " + TestRunResultAccumulator.getInstance().getTestClassName());
            } else {
                getLogger().severe("Failed to upload test results!\nmessage: " + result.getMessage());
            }

            TestRunResultAccumulator.getInstance().clear();
        }

    }

    public TestResultSender getTestResultSender() {
        if(testResultSender == null)
            testResultSender = new TestResultSender(new GutsHttpClient());

        return testResultSender;
    }

    /**
     * Get current JUnit 5 {@link TestPlan}
     * @return current JUnit 5 {@link TestPlan}
     */
    public static TestPlan getTestPlan() {
        return testPlan;
    }

    /**
     * Get logger
     * @return Logger
     */
    public static Logger getLogger() {
        if(logger == null) {
            logger = Logger.getLogger(GutsJUnit5.class.getName());
            logger.setUseParentHandlers(false);
            ConsoleHandler handler = new ConsoleHandler();
            handler.setFormatter(new SimpleFormatter() {
                private static final String format = " [Guts - %1$s] %2$s %n";
                @Override
                public String format(LogRecord record) {
                    return String.format(format,
                            record.getLevel().getLocalizedName(),
                            record.getMessage()
                    );
                }
            });
            logger.addHandler(handler);
        }

        return logger;
    }

}
