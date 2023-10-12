package be.pxl.guts.junit_5.util;

import be.pxl.guts.core.models.SolutionFile;
import be.pxl.guts.core.util.FileUtil;
import be.pxl.guts.core.util.TestAccumulator;
import be.pxl.guts.junit_5.GutsFixture;

import java.util.List;

/**
 * Extension class for adding details from {@link GutsFixture} to {@link TestAccumulator}
 */
public class JUnitTestAccumulator extends TestAccumulator {

    /**
     * Default constructor
     *
     * @param fixture fixture containing details to add to test accumulator
     */
    public JUnitTestAccumulator(GutsFixture fixture, List<SolutionFile> solutionFiles, String hashPath) {
        super(fixture.courseCode(),
                fixture.chapterCode(),
                fixture.exerciseCode(),
                fixture.testRunType(),
                solutionFiles,
                FileUtil.getFileHash(hashPath)
        );
    }

}
