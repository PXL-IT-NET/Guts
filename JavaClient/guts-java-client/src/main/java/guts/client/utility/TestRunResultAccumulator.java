package guts.client.utility;

import org.junit.jupiter.api.Disabled;
import org.junit.jupiter.api.RepeatedTest;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.platform.engine.support.descriptor.ClassSource;
import guts.client.Configuration;
import guts.client.models.TestResult;

import java.io.File;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public class TestRunResultAccumulator {

    private static TestRunResultAccumulator instance;

    private List<TestResult> testResults;
    private int numberOfTestInCurrentFixture;
    private String testClassName;
    private String testCodeHash;

    private TestRunResultAccumulator() {
        testResults = new ArrayList<>();
        clear();
    }

    public void addTestResult(TestResult result) {
        if(testResults.stream().anyMatch(t -> t.getTestName().equalsIgnoreCase(result.getTestName())))
            return;

        testResults.add(result);
    }

    public void setupMetaData(ClassSource testClass) {
        if(numberOfTestInCurrentFixture > 0)
            return;

        numberOfTestInCurrentFixture = Math.toIntExact(Arrays.asList(testClass.getJavaClass().getMethods()).stream().filter(method ->
                (method.isAnnotationPresent(Test.class) || method.isAnnotationPresent(RepeatedTest.class) || method.isAnnotationPresent(ParameterizedTest.class))
                            && !method.isAnnotationPresent(Disabled.class
                )).count());
        testClassName = testClass.getJavaClass().getSimpleName();
        testCodeHash = FileUtil.getFileHash(Configuration.getTestDirectory() + testClass.getJavaClass().getPackage().getName().replace(".", "/") + File.separator + testClass.getJavaClass().getSimpleName() + ".java");
    }

    public void clear() {
        this.testResults.clear();
        this.numberOfTestInCurrentFixture = 0;
        this.testClassName = "";
        this.testCodeHash = "";
    }

    public boolean allTestOfFixtureWereRunned() {
        return testResults.size() >= numberOfTestInCurrentFixture;
    }

    public List<TestResult> getTestResults() {
        return testResults;
    }

    public String getTestClassName() {
        return testClassName;
    }

    public String getTestCodeHash() {
        return testCodeHash;
    }

    public int getNumberOfTestInCurrentFixture() {
        return numberOfTestInCurrentFixture;
    }

    public static TestRunResultAccumulator getInstance() {
        if(instance == null)
            instance = new TestRunResultAccumulator();

        return instance;
    }
}
