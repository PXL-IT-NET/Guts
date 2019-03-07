import guts.client.annotations.GutsFixture;
import org.junit.jupiter.api.*;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

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
    @DisplayName("Parameterized Test")
    @ValueSource(strings = { "param1", "param2", "param3"})
    public void parameterizedTest(String param) {
        assertNotNull(param);
    }

    @RepeatedTest(value = 5, name = "{displayName} {currentRepetition}/{totalRepetitions}")
    @DisplayName("Repeating Test")
    public void repeatedTest(RepetitionInfo repInfo, TestInfo testInfo) {
        System.out.println(testInfo.getDisplayName() + "-->" + repInfo.getCurrentRepetition());
        boolean greater = repInfo.getCurrentRepetition() >= 2;
        assertTrue(greater, "Repetition should be greater or equal to 2");
    }


}