import be.pxl.guts.junit_5.GutsFixture;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.extension.ExtensionContext;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.ArgumentsProvider;
import org.junit.jupiter.params.provider.ArgumentsSource;
import org.junit.jupiter.params.provider.EnumSource;

import java.util.stream.Stream;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

@GutsFixture(courseCode = "java1", chapterCode = "H05", exerciseCode = "Exercises02", sourceCodeRelativeFilePaths = "Calculator.java")
class CalculatorTest2 {

    @Test
    @DisplayName("Calculator needs to be able to add numbers")
    public void add() {
        Calculator2 calc = new Calculator2();
        int result = calc.add(5, 2);

        assertEquals(7, result, "Calculator2.add(5, 2) should return 7");
    }

    @Test
    @DisplayName("Calculator needs to be able to subtract numbers")
    public void sub() {
        Calculator2 calc = new Calculator2();
        int result = calc.sub(5, 3);

        assertEquals(2, result, "The result of Calculator2.sub(5, 3) should be 2 but is " + result);
    }

    @RepeatedTest(value = 5, name = "{displayName} {currentRepetition}/{totalRepetitions}")
    @DisplayName("Repeating Test")
    public void repeatedTest(RepetitionInfo repInfo, TestInfo testInfo) {
        assertTrue(true, "Should be true");
    }

    @ParameterizedTest()
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
        Calculator2 calc = new Calculator2();
        int result = calc.add(num1, num2);

        assertEquals(expected, result, "Calculator2.add(" + num1 + ", " + num2 +") should return " + expected);
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

}