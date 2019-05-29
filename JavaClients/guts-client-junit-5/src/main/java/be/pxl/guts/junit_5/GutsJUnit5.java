package be.pxl.guts.junit_5;

import be.pxl.guts.core.GutsCore;
import be.pxl.guts.core.http.ApiResult;
import be.pxl.guts.core.util.CodeCleaner;
import be.pxl.guts.core.util.FileUtil;
import be.pxl.guts.core.util.TestAccumulator;
import be.pxl.guts.junit_5.util.JUnitTestAccumulator;
import org.junit.platform.engine.TestExecutionResult;
import org.junit.platform.engine.TestSource;
import org.junit.platform.engine.support.descriptor.ClassSource;
import org.junit.platform.launcher.TestExecutionListener;
import org.junit.platform.launcher.TestIdentifier;

import java.io.File;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Arrays;
import java.util.List;
import java.util.stream.Collectors;

public class GutsJUnit5 implements TestExecutionListener {

    private GutsCore gutsCore;
    private TestIdentifier currentTestIdentifier;
    private TestAccumulator testAccumulator;

    public GutsJUnit5() {
        this.gutsCore = new GutsCore();
    }

    @Override
    public void executionStarted(TestIdentifier testIdentifier) {
        if(!testIdentifier.getSource().isPresent()) {
            return;
        }

        TestSource testSource = testIdentifier.getSource().get();
        if(testSource instanceof ClassSource) {

            ClassSource classSource = (ClassSource) testSource;
            GutsFixture fixture = classSource.getJavaClass().getAnnotation(GutsFixture.class);

            if(fixture != null) {
                this.currentTestIdentifier = testIdentifier;

                Path testCodePath = Paths.get(gutsCore.getConfig().getTestDirectory(), // test directory
                        classSource.getJavaClass().getPackage().getName().replace(".", File.separator), // package to path
                        classSource.getJavaClass().getSimpleName() + ".java"); // .java source file

                String sourceCode = FileUtil.getSourceCodeFiles(gutsCore.getConfig().getSourceDirectory(), fixture.sourceCodeRelativeFilePaths());
                sourceCode = CodeCleaner.removeCommentsAndRemoveMultipleBlankLines(sourceCode);

                this.testAccumulator = new JUnitTestAccumulator(fixture, sourceCode, testCodePath.toString());
            }
        }
    }

    @Override
    public void executionFinished(TestIdentifier testIdentifier, TestExecutionResult testExecutionResult) {

        if(testIdentifier.equals(currentTestIdentifier)) {
            ClassSource classSource = (ClassSource) testIdentifier.getSource().get();
            GutsFixture fixture = classSource.getJavaClass().getAnnotation(GutsFixture.class);

            if(fixture != null) {
                ApiResult result = gutsCore.sendResults(testAccumulator);
                if(result.isSuccessful()) {
                    gutsCore.getLogger().info("Successfully uploaded results for '" + classSource.getJavaClass().getSimpleName() + "' !");
                } else {
                    gutsCore.getLogger().severe("Failed to upload results for '" + classSource.getJavaClass().getSimpleName() + "'\nMessage: " + result.getMessage());
                }
            }
        }

        if(!testIdentifier.isTest()) {
            return;
        }

        String testName = testIdentifier.getDisplayName();
        boolean passed = testExecutionResult.getStatus() == TestExecutionResult.Status.SUCCESSFUL;
        String throwableMessage = testExecutionResult.getThrowable().isPresent() ? testExecutionResult.getThrowable().get().toString().substring(37) : "";
        this.testAccumulator.addTestResult(testName, passed, throwableMessage);
    }
}
