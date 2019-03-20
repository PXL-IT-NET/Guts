import guts.client.annotations.GutsFixture;
import org.junit.jupiter.api.*;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.junit.jupiter.params.provider.ValueSource;

import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.*;

@GutsFixture(courseCode = "java1", chapterCode = "H01", exerciseCode = "Exercises01", sourceCodeRelativeFilePaths = "Calculator.java")
class CalculatorTest {
    @Test
    @DisplayName("Calculator needs to be able to add numbers")
    public void add() {
        Calculator calc = new Calculator();
        int result = calc.add(5, 2);

        assertEquals(7, result);
    }

    @Test
    @DisplayName("Calculator needs to be able to subtract numbers")
    public void sub() {
        Calculator calc = new Calculator();
        int result = calc.sub(5, 3);

        assertEquals(2, result, "The result of sub(5, 3) should be 2 but is " + result);
    }

    @ParameterizedTest(name = "{displayName} [{index}] {arguments}")
    @DisplayName("Parameterized addition test")
    @MethodSource("provideNumbersForAdditionParameterizedTest")
    public void additionParameterizedTest(int num1, int num2, int expected) {
        Calculator calc = new Calculator();
        int result = calc.add(num1, num2);

        assertEquals(expected, result);
    }

    private static Stream<Arguments> provideNumbersForAdditionParameterizedTest() {
        return Stream.of(
                Arguments.of(2, 3, 5),
                Arguments.of(6, 5, 11),
                Arguments.of(7, 8, 15),
                Arguments.of(2000, 6000, 8000),
                Arguments.of(52551, 2568, 55119)
        );
    }

    @RepeatedTest(value = 5, name = "{displayName} {currentRepetition}/{totalRepetitions}")
    @DisplayName("Repeating Test")
    public void repeatedTest(RepetitionInfo repInfo, TestInfo testInfo) {
        System.out.println(testInfo.getDisplayName() + "-->" + repInfo.getCurrentRepetition());
        boolean greater = repInfo.getCurrentRepetition() >= 2;
        assertTrue(greater, "Repetition should be greater or equal to 2");
    }


}