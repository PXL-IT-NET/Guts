package guts.client.utility;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.assertEquals;

class CodeCleanerTest {

    @ParameterizedTest
    @MethodSource("provideStringsForShouldRemoveBlockComments")
    void shouldRemoveBlockComments(String code, String expected) {
        assertEquals(expected, CodeCleaner.removeComments(code));
    }

    @ParameterizedTest
    @MethodSource("provideStringsForShouldRemoveLineComments")
    void shouldRemoveLineComments(String code, String expected) {
        assertEquals(expected, CodeCleaner.removeComments(code));
    }

    @ParameterizedTest
    @MethodSource("provideStringsForShouldRemoveMultipleBlankLines")
    void shouldRemoveMultipleBlankLines(String code, String expected) {
        assertEquals(expected, CodeCleaner.removeMultipleBlankLines(code));
    }

    private static Stream<Arguments> provideStringsForShouldRemoveBlockComments() {
        return Stream.of(
                Arguments.of("var a = 3;/*Block \n comment*/var b = 3;", "var a = 3;var b = 3;"),
                Arguments.of("var a = 3;/*Block //nested comment\n comment*/var b = 3;", "var a = 3;var b = 3;")
        );
    }

    private static Stream<Arguments> provideStringsForShouldRemoveLineComments() {
        return Stream.of(
                Arguments.of("var a = 1;\n//last line is comment", "var a = 1;\n"),
                Arguments.of("//commented line\nvar a = 2;", "\nvar a = 2;"),
                Arguments.of("var a = 3;//Comment after code\nvar b = 3;", "var a = 3;\nvar b = 3;")
        );
    }

    private static Stream<Arguments> provideStringsForShouldRemoveMultipleBlankLines() {
        return Stream.of(
                Arguments.of("line1\n\nline2", "line1\n\nline2"),
                Arguments.of("line1\n\n\n\nline2", "line1\n\nline2"),
                Arguments.of("line1\n\n\n\n\n\n\n\nline2", "line1\n\nline2")
        );
    }

}