package guts.client.models;

import guts.client.annotations.GutsFixture;

public class Assignment {

    private String courseCode;
    private String topicCode;
    private String assignmentCode;

    public Assignment(GutsFixture fixture) {
        this(fixture.courseCode(), fixture.chapterCode(), fixture.exerciseCode());
    }

    public Assignment(String courseCode, String topicCode, String assignmentCode) {
        this.courseCode = courseCode;
        this.topicCode = topicCode;
        this.assignmentCode = assignmentCode;
    }

}
