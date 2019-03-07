package guts.client.annotations;

import guts.client.enums.TestRunType;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * Annotation to setup properties that specify what test are located in the file
 */
@Target(ElementType.TYPE)
@Retention(RetentionPolicy.RUNTIME)
public @interface GutsFixture{

    /**
     * The identifier of the course.
     *
     * @return the course identifier
     */
    String courseCode();

    /**
     * The identifier of the chapter in the course.
     *
     * @return the chapter identifier
     */
    String chapterCode();

    /**
     * The identifier of the exercise in the chapter.
     *
     * @return the exercise identifier
     */
    String exerciseCode();

    /**
     * The paths to source code files that need to be uploaded to the api.
     * Separated by a semicolon
     *
     * @return String of paths seperated by a semicolon
     */
    String sourceCodeRelativeFilePaths() default "";

    /**
     * The type of test this is.
     * A test for exercises or projects.
     *
     * see {@link TestRunType}
     *
     * @return the test type
     */
    TestRunType testRunType() default TestRunType.ForExercises;

}
