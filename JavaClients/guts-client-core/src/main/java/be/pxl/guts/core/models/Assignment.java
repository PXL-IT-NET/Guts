package be.pxl.guts.core.models;

public class Assignment {

    private String courseCode;
    private String topicCode;
    private String assignmentCode;

    public Assignment(String courseCode, String topicCode, String assignmentCode) {
        this.courseCode = courseCode;
        this.topicCode = topicCode;
        this.assignmentCode = assignmentCode;
    }

    public String getCourseCode() {
        return courseCode;
    }

    public String getTopicCode() {
        return topicCode;
    }

    public String getAssignmentCode() {
        return assignmentCode;
    }

}
