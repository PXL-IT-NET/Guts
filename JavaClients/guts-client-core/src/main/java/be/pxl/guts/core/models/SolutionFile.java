package be.pxl.guts.core.models;

import be.pxl.guts.core.util.CodeCleaner;

public class SolutionFile {
    private String filePath;
    private String content;

    public SolutionFile(String filePath, String content) {
        this.filePath = filePath;
        this.content = content;
    }

    public String getFilePath() {
        return filePath;
    }

    public String getContent() {
        return content;
    }

    public void setFilePath(String filePath) {
        this.filePath = filePath;
    }

    public void setContent(String content) {
        this.content = content;
    }

    public void cleanCode() {
        this.content = CodeCleaner.removeCommentsAndRemoveMultipleBlankLines(this.content);
    }
}
