using System.Collections.Generic;
using System.Linq;
using Guts.Domain.TestAggregate;
using Guts.Domain.TopicAggregate;

namespace Guts.Domain.AssignmentAggregate;

public static class AssignmentRecommendationPolicy
{
    public static IReadOnlyList<RecommendedAssignment> RecommendAssignments(
        IReadOnlyList<ITopic> topics,
        IReadOnlySet<int> passedTestIds,
        int maxCount = 2)
    {
        if (topics == null || topics.Count == 0 || maxCount <= 0)
        {
            return [];
        }

        passedTestIds ??= new HashSet<int>();

        var inProgress = new List<RecommendedAssignment>();
        var untouched = new List<RecommendedAssignment>();

        foreach (var topic in topics)
        {
            foreach (var assignment in topic.Assignments ?? Enumerable.Empty<Assignment>())
            {
                var tests = assignment.Tests ?? [];
                if (tests.Count == 0)
                {
                    continue;
                }

                int passedCount = tests.Count(t => passedTestIds.Contains(t.Id));
                if (passedCount == tests.Count)
                {
                    continue;
                }

                var recommended = new RecommendedAssignment(
                    assignment.Id,
                    assignment.Code,
                    assignment.Description,
                    topic.Code?.ToString() ?? string.Empty,
                    topic.Description,
                    passedCount,
                    tests.Count);

                if (passedCount > 0)
                {
                    inProgress.Add(recommended);
                }
                else
                {
                    untouched.Add(recommended);
                }
            }
        }

        return inProgress.Concat(untouched).Take(maxCount).ToList();
    }
}
