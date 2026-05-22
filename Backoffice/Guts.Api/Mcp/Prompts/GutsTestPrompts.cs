using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Guts.Api.Mcp.Prompts;

[McpServerPromptType]
public class GutsTestPrompts
{

    [McpServerPrompt(Name = "exercise_tests_xunit")]
    [Description("""
                 Pre-built instructions to generate GUTS tests for a SUT (system under test) class of an individual exercise, using the xUnit framework. 
                 Provide the name of the SUT class that is (part of) the exercise.
                 """)]
    public string CreateGutsXUnitTestsForExercise(string sutClassName)
    {
        return CreateGutsTests(sutClassName, "xUnit", true);
    }

    [McpServerPrompt(Name = "exercise_tests_nunit")]
    [Description("""
                 Pre-built instructions to generate GUTS tests for a SUT (system under test) class of an individual exercise, using the nUnit framework. 
                 Provide the name of the SUT class that is (part of) the exercise.
                 """)]
    public string CreateGutsNUnitTestsForExercise(string sutClassName)
    {
        return CreateGutsTests(sutClassName, "nUnit", true);
    }

    [McpServerPrompt(Name = "projectcomponent_tests_xunit")]
    [Description("""
                 Pre-built instructions to generate GUTS tests for a SUT (system under test) class of a project component, using the xUnit framework. 
                 Provide the name of the SUT class that is (part of) the project component.
                 """)]
    public string CreateGutsXUnitTestsForProjectComponent(string sutClassName)
    {
        return CreateGutsTests(sutClassName, "xUnit", false);
    }

    [McpServerPrompt(Name = "projectcomponent_tests_nunit")]
    [Description("""
                 Pre-built instructions to generate GUTS tests for a SUT (system under test) class of a project component, using the nUnit framework. 
                 Provide the name of the SUT class that is (part of) the project component.
                 """)]
    public string CreateGutsNUnitTestsForProjectComponent(string sutClassName)
    {
        return CreateGutsTests(sutClassName, "nUnit", false);
    }

    private string CreateGutsTests(string sutClassName, string testFramework, bool isIndividualExercise)
    {
        string testFixtureAttribute = isIndividualExercise ? "ExerciseTestFixture" : "ProjectComponentTestFixture";
        string topicParameter = isIndividualExercise ? "chapterCode" : "projectCode";
        string assignmentParameter = isIndividualExercise ? "exerciseCode" : "projectComponentCode";
        string testAttribute = testFramework == "xUnit" ? "[MonitoredFact]/[MonitoredTheory]" : "[MonitoredTest]";
        string testCaseAttribute = testFramework == "xUnit" ? "[InlineData]" : "[TestCase]";

        return $"""
                You are generating GUTS monitored tests for the class `{sutClassName}`.

                Follow these instructions:
                1. Start by identifying the public behavior of `{sutClassName}` and list test scenarios for success, edge, and failure cases.
                3. Read the instructions on http://guts-web.pxl.be/teacher-docs/{testFramework.ToLower()} or guts://guides/{testFramework.ToLower()}-attributes (mcp) to understand which attributes to use for the test class and methods.
                3. Identify the test project and test class where the tests for `{sutClassName}` should be implemented, based on the project structure and naming conventions. 
                   Create a {testFramework} test project and/or test class if it does not exist yet and add the necessary GUTS NuGet package.
                4. Check if there are existing tests in the test project that can be used as a reference for structure and style.
                5. Use GUTS monitored attributes:
                   - [{testFixtureAttribute}(...)], plus {testAttribute}.
                   - Use the same `courseCode` and `{topicParameter}` as other tests in the same test project if there are any. If there are none or multiple, use `TODO_courseCode` and `TODO_{topicParameter}` or ask for clarification.
                   - For the `{assignmentParameter}` parameter, derive a code from the SUT class name.
                   - Include the path to the SUT class file in the attribute (relative to the solution directory).
                   - Put the attribute on the test class in comment so that the test can be run without sending the results to the GUTS server until it's ready.
                6. Prefer clear test names that describe behavior; keep Arrange/Act/Assert structure explicit.
                7. Do not use "sut" as a variable name in the tests; use descriptive names that reflect the role of production code class in the test scenario.
                8. Include data-driven cases where relevant ({testCaseAttribute}).
                9. Run the tests locally to ensure they pass before finalizing the code.
                10. Ensure the final code is clean, well-structured and with minimal duplication, following best practices for test code.

                Output format:
                - First: a short test plan for `{sutClassName}`.
                - Then: complete compilable test class code using monitored GUTS attributes.
                - Finally: give the suggestion to uncomment the GUTS attribute on the test class when you are ready to send results to the GUTS server (and thus create the tests if you are a teacher).
                """;
    }
}
