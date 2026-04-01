using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Guts.Api.Mcp.Prompts;

[McpServerPromptType]
public class GutsTestPrompts
{
    [McpServerPrompt(Name = "create_guts_tests_for_exercise")]
    [Description("Pre-built instructions to generate GUTS tests from a SUT class that is part of an individual exercise")]
    public string CreateGutsTestsForExercise(string sutClassName, string courseCode, string exerciseCode)
    {
        return CreateGutsTests(sutClassName, courseCode, exerciseCode, isExercise: true);
    }

    [McpServerPrompt(Name = "create_guts_tests_for_project_component")]
    [Description("Pre-built instructions to generate GUTS tests from a SUT class that is part of a project component")]
    public string CreateGutsTestsForProjectComponent(string sutClassName, string courseCode, string projectComponentCode)
    {
        return CreateGutsTests(sutClassName, courseCode, projectComponentCode, isExercise: false);
    }

    private string CreateGutsTests(string sutClassName, string courseCode, string assignmentCode, bool isExercise)
    {
        string testFixtureAttribute = isExercise ? "ExerciseTestFixture" : "ProjectComponentTestFixture";
        string testClassAttribute = isExercise ? "ExerciseTestClass" : "ProjectComponentTestClass";
        return $"""
                You are generating GUTS monitored tests for the class `{sutClassName}`.

                Follow these instructions:
                1. Check if NUnit or xUnit is used in the project to determine which attributes to use for the test class and methods.
                    - NUnit: read guts://guides/nunit-attributes.
                    - xUnit: read guts://guides/xunit-attributes.
                2. Start by identifying the public behavior of `{sutClassName}` and list test scenarios for success, edge, and failure cases.
                3. Check if there are existing tests in the test project that can be used as a reference for structure and style.
                4. Use GUTS monitored attributes:
                   - NUnit: [{testFixtureAttribute}(...)], plus [MonitoredTest].
                   - xUnit: [{testClassAttribute}(...)], plus [MonitoredFact]/[MonitoredTheory].
                   - Use {courseCode} and {assignmentCode} in the attributes to link tests to the correct exercise.
                   - Include the path to the SUT class file in the attribute (relative to the solution directory).
                   - Put the attribute in comment so that the test can be run without sending the results to the GUTS server until it's ready.
                5. Prefer clear test names that describe behavior; keep Arrange/Act/Assert structure explicit.
                6. Include data-driven cases where relevant ([TestCase] or [InlineData]).
                7. Run the tests locally to ensure they pass before finalizing the code.
                8. Ensure the final code is clean and well-structured, following best practices for test code.

                Output format:
                - First: a short test plan for `{sutClassName}`.
                - Finally: complete compilable test class code using monitored GUTS attributes.
                """;
    }
}
