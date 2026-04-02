using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Guts.Api.Mcp.Prompts;

[McpServerPromptType]
public class GutsTestPrompts
{
    [McpServerPrompt(Name = "create_guts_tests")]
    [Description("""
                 Pre-built instructions to generate GUTS tests for a SUT (system under test) class. 
                 Provide the name of the SUT class and whether it's part of an individual exercise or a project for teams.
                 """)]
    public string CreateGutsTests(string sutClassName, bool isIndividualExercise = true)
    {
        string testFixtureAttribute = isIndividualExercise ? "ExerciseTestFixture" : "ProjectComponentTestFixture";
        string testClassAttribute = isIndividualExercise ? "ExerciseTestClass" : "ProjectComponentTestClass";
        string topicParameter = isIndividualExercise ? "chapterCode" : "projectCode";
        string assignmentParameter = isIndividualExercise ? "exerciseCode" : "projectComponentCode";
        return $"""
                You are generating GUTS monitored tests for the class `{sutClassName}`.

                Follow these instructions:
                1. Start by identifying the public behavior of `{sutClassName}` and list test scenarios for success, edge, and failure cases.
                2. Identify the test project and test class where the tests for `{sutClassName}` should be implemented, based on the project structure and naming conventions.
                3. Check if NUnit or xUnit is used in the test project to determine which attributes to use for the test class and methods.
                    - NUnit: read guts://guides/nunit-attributes.
                    - xUnit: read guts://guides/xunit-attributes.
                4. Check if there are existing tests in the test project that can be used as a reference for structure and style.
                5. Use GUTS monitored attributes:
                   - NUnit: [{testFixtureAttribute}(...)], plus [MonitoredTest].
                   - xUnit: [{testClassAttribute}(...)], plus [MonitoredFact]/[MonitoredTheory].
                   - Use the same `courseCode` and `{topicParameter}` as other tests in the same test project if there are any. If there are none or multiple, use `TODO_courseCode` and `TODO_{topicParameter}`.
                   - For the `{assignmentParameter}` parameter, derive a code from the SUT class name.
                   - Include the path to the SUT class file in the attribute (relative to the solution directory).
                   - Put the attribute on the test class in comment so that the test can be run without sending the results to the GUTS server until it's ready.
                6. Prefer clear test names that describe behavior; keep Arrange/Act/Assert structure explicit.
                7. Do not use "sut" as a variable name in the tests; use descriptive names that reflect the role of production code class in the test scenario.
                8. Include data-driven cases where relevant ([TestCase] or [InlineData]).
                9. Run the tests locally to ensure they pass before finalizing the code.
                10. Ensure the final code is clean, well-structured and with minimal duplication, following best practices for test code.

                Output format:
                - First: a short test plan for `{sutClassName}`.
                - Then: complete compilable test class code using monitored GUTS attributes.
                - Finally: give the suggestion to uncomment the GUTS attribute on the test class when you are ready to send results to the GUTS server (and thus create the tests if you are a lector).
                """;
    }
}
