using System;

namespace Guts.Client.Classic
{
    [Obsolete("Use 'ExerciseTestFixtureAttribute' instead"), AttributeUsage(AttributeTargets.Class)]
    public class MonitoredTestFixtureAttribute : ExerciseTestFixtureAttribute
    {
        public MonitoredTestFixtureAttribute(string courseCode, string chapterCode, string exerciseCode) : base(courseCode, chapterCode, exerciseCode)
        {
        }

        public MonitoredTestFixtureAttribute(string courseCode, string chapterCode, string exerciseCode, string sourceCodeRelativeFilePaths) : base(courseCode, chapterCode, exerciseCode, sourceCodeRelativeFilePaths)
        {
        }
    }
}