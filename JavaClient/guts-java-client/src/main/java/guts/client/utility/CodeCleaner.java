package guts.client.utility;

public final class CodeCleaner {

    private CodeCleaner() {}

    public static String removeComments(String code) {
        return code.replaceAll("(/\\*([^*]|[\\r\\n]|(\\*+([^*/]|[\\r\\n])))*\\*+/|[\\t]*//.*)|\"(\\\\.|[^\\\\\"])*\"|'(\\\\[\\s\\S]|[^'])*'", "");
    }

    public static String removeMultipleBlankLines(String code) {
        return code.replaceAll("(?m)^[ \t]*\r?\n\n", "");
    }

    public static String removeCommentsAndRemoveMultipleBlankLines(String code) {
        code = removeComments(code);
        code = removeMultipleBlankLines(code);
        return code;
    }

}
