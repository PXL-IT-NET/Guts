import { Component } from "@angular/core";

@Component({
  standalone: false,
  selector: "app-teacher-docs-junit",
  templateUrl: "./teacher-docs-junit.component.html",
})
export class TeacherDocsJunitComponent {
  public readonly minimalExampleCode = `@GutsFixture(
    courseCode = "javaEss",
    chapterCode = "C1",
    exerciseCode = "Exercise01",
    sourceCodeRelativeFilePaths = "Calculator.java")
class CalculatorTests {

    @Test
    @DisplayName("Calculator needs to be able to add numbers")
    void add_shouldReturnExpectedValue() {
        Calculator calculator = new Calculator();

        int result = calculator.add(5, 2);

        assertEquals(7, result, "Calculator.add(5, 2) should return 7");
    }

      @ParameterizedTest
      @ValueSource(ints = { 0, 1, 5 })
      @DisplayName("Calculator returns provided value")
      void echo_shouldReturnInput(int value) {
        Calculator calculator = new Calculator();

        assertEquals(value, calculator.echo(value), "echo should return the same value");
      }
}`;
}
