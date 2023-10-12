import be.pxl.guts.junit_5.GutsFixture;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtensionContext;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.*;

import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.*;

@GutsFixture(courseCode = "javaEss1", chapterCode = "HT1", exerciseCode = "Exercises01", sourceCodeRelativeFilePaths = "Calculator.java")
class CalculatorTest {

    @Test
    @DisplayName("Calculator needs to be able to add numbers")
    public void add() {
        Calculator calc = new Calculator();
        int result = calc.add(5, 2);

        assertEquals(7, result, "Calculator.add(5, 2) should return 7");
    }

    @Test
    @DisplayName("Calculator needs to be able to subtract numbers")
    public void sub() {
        Calculator calc = new Calculator();
        int result = calc.sub(5, 3);

        assertEquals(2, result, "The result of sub(5, 3) should be 2 but is " + result);
    }

    @RepeatedTest(value = 5, name = "{displayName} {currentRepetition}/{totalRepetitions}")
    @DisplayName("Repeating Test")
    public void repeatedTest(RepetitionInfo repInfo, TestInfo testInfo) {
        assertTrue(true, "Should be true");
    }

    @ParameterizedTest(name = "{displayName} [{index}] {arguments}")
    @EnumSource(value = TestEnum.class, names = "Some.+", mode = EnumSource.Mode.MATCH_ALL)
    public void EnumTest(TestEnum value) {
        assertTrue(true, "Should be true");
    }

    public enum TestEnum {
        Some,
        SomeOther,
        SomeThing,
        ThingSomee
    }

    @ParameterizedTest(name = "{displayName} [{index}] {arguments}")
    @DisplayName("Parameterized addition test")
    @ArgumentsSource(NumberArgumentsProvider.class)
    public void additionParameterizedTestClass(int num1, int num2, int expected) {
        Calculator calc = new Calculator();
        int result = calc.add(num1, num2);

        assertEquals(expected, result, "Calculator.add(" + num1 + ", " + num2 +") should return " + expected);
    }

    static class NumberArgumentsProvider implements ArgumentsProvider {
        @Override
        public Stream<? extends Arguments> provideArguments(ExtensionContext context) throws Exception {
            return Stream.of(
                    Arguments.of(2, 3, 5),
                    Arguments.of(6, 5, 11),
                    Arguments.of(7, 8, 15),
                    Arguments.of(2000, 6000, 8000),
                    Arguments.of(52551, 2568, 55119)
            );
        }
    }

    @ParameterizedTest(name = "{displayName} [{index}] {arguments}")
    @MethodSource("provideStringsForIsBlank")
    void isBlank_ShouldReturnTrueForNullOrBlankStrings(String input, boolean expected) {
        assertEquals(expected, input == null || input.trim().equals(""));
    }

    private static Stream<Arguments> provideStringsForIsBlank() {
        return Stream.of(
                Arguments.of(null, true),
                Arguments.of("", true),
                Arguments.of("  ", true),
                Arguments.of("not blank", false)
        );
    }

    @ParameterizedTest(name = "{displayName} [{index}] {arguments}")
    @ValueSource(ints= {1, 3, 5, 15})
    public void shouldReturnTrueForOddNumbers(int number) {
        assertTrue(number % 2 == 1);
    }

    @ParameterizedTest(name = "{displayName} [{index}] {arguments}")
    @CsvSource({"test,TEST", "tEst,TEST", "Java,JAVA"})
    void toUpperCase_ShouldGenerateTheExpectedUppercaseValue(String input, String expected) {
        String actualValue = input.toUpperCase();
        assertEquals(expected, actualValue);
    }

    @ParameterizedTest(name = "{displayName} [{index}] {arguments}")
    @CsvFileSource(resources = "/data.csv", numLinesToSkip = 1)
    void toUpperCase_ShouldGenerateTheExpectedUppercaseValueCSVFile(String input, String expected) {
        String actualValue = input.toUpperCase();
        assertEquals(expected, actualValue);
    }

}