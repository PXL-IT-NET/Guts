using System.Collections.Generic;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;

namespace Guts.Domain.CourseAggregate;

public interface ICourseProgressCalculator
{
    CourseProgressReport CalculateProgress(
        Course course,
        IReadOnlyCollection<Chapter> chapters,
        IReadOnlyCollection<TestResult> testResults);
}

