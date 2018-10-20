using System;

namespace Guts.Client.Classic
{
    [Obsolete("Use 'ExerciseTestFixtureAttribute' instead"), AttributeUsage(AttributeTargets.Class)]
    public class MonitoredTestFixtureAttribute : ExerciseTestFixtureAttribute
    {
        public MonitoredTestFixtureAttribute(string courseCode, int chapter, string exerciseCode) : base(courseCode, chapter, exerciseCode)
        {
        }

        public MonitoredTestFixtureAttribute(string courseCode, int chapter, string exerciseCode, string sourceCodeRelativeFilePaths) : base(courseCode, chapter, exerciseCode, sourceCodeRelativeFilePaths)
        {
        }
    }
}