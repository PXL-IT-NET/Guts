package be.pxl.guts.core.util;

/**
 * Utility class to clean source code collected by client
 */
public class CodeCleaner {

    private CodeCleaner() {}

    /**
     * Remove comments from source code.
     *
     * @param code source code where comments will be removed
     * @return code without any comments
     */
    public static String removeComments(String code) {
        return code.replaceAll("(/\\*([^*]|[\\r\\n]|(\\*+([^*/]|[\\r\\n])))*\\*+/|[\\t]*//.*)|\"(\\\\.|[^\\\\\"])*\"|'(\\\\[\\s\\S]|[^'])*'", "");
    }

    /**
     * Replace multiple blank lines by one blank line.
     *
     * @param code source code where multiple blank lines will be replaced
     * @return code without multiple blank lines
     */
    public static String removeMultipleBlankLines(String code) {
        return code.replaceAll("(?m)^[ \t]*\r?\n\n", "");
    }

    /**
     * Remove comments and multiple blank lines from source code.
     *
     * @param code source code to be sanitized
     * @return sanitized code
     */
    public static String removeCommentsAndRemoveMultipleBlankLines(String code) {
        code = removeComments(code);
        code = removeMultipleBlankLines(code);
        return code;
    }

}
