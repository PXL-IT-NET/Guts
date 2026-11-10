using System.Collections.Generic;
using System.Linq;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.AssignmentAggregate;

[TestFixture]
public class AssignmentRecommendationPolicyTests
{
    [Test]
    public void RecommendAssignments_NullOrEmptyTopics_ReturnsEmpty()
    {
        Assert.That(AssignmentRecommendationPolicy.RecommendAssignments(null, new HashSet<int>()), Is.Empty);
        Assert.That(AssignmentRecommendationPolicy.RecommendAssignments([], new HashSet<int>()), Is.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void RecommendAssignments_ZeroOrNegativeMaxCount_ReturnsEmpty(int maxCount)
    {
        var chapter = new Chapter { Id = 1, Code = "ch01", Description = "Chapter 1" };
        Assert.That(AssignmentRecommendationPolicy.RecommendAssignments([chapter], new HashSet<int>(), maxCount), Is.Empty);
    }

    [Test]
    public void RecommendAssignments_SkipsAssignmentsWithNoTestsAndCompletedAssignments()
    {
        var noTestsAssignment = new AssignmentBuilder().WithId(1).WithCode("no-tests").Build();
        noTestsAssignment.ClearTests();

        var completedAssignment = new AssignmentBuilder().WithId(2).WithCode("completed").WithRandomTests(2).Build();
        var completedTestIds = completedAssignment.Tests.Select(t => t.Id).ToHashSet();

        var inProgressAssignment = new AssignmentBuilder().WithId(3).WithCode("in-progress").WithRandomTests(3).Build();
        var partialTestId = inProgressAssignment.Tests.First().Id;

        var chapter = new Chapter
        {
            Id = 10,
            Code = "ch01",
            Description = "Chapter 1",
            Assignments = [noTestsAssignment, completedAssignment, inProgressAssignment]
        };

        var passedTestIds = new HashSet<int>(completedTestIds) { partialTestId };

        var recommendations = AssignmentRecommendationPolicy.RecommendAssignments([chapter], passedTestIds, 5);

        Assert.That(recommendations, Has.Count.EqualTo(1));
        var item = recommendations[0];
        Assert.That(item.AssignmentId, Is.EqualTo(inProgressAssignment.Id));
        Assert.That(item.AssignmentCode, Is.EqualTo("in-progress"));
        Assert.That(item.TopicCode, Is.EqualTo("ch01"));
        Assert.That(item.GreenTestsCount, Is.EqualTo(1));
        Assert.That(item.TotalTestsCount, Is.EqualTo(3));
    }

    [Test]
    public void RecommendAssignments_PrioritizesInProgressOverUntouchedAndLimitsToMaxCount()
    {
        var inProgressAssignment1 = new AssignmentBuilder().WithId(1).WithCode("prog1").WithRandomTests(4).Build();
        var inProgressAssignment2 = new AssignmentBuilder().WithId(2).WithCode("prog2").WithRandomTests(4).Build();
        var untouchedAssignment = new AssignmentBuilder().WithId(3).WithCode("untouched").WithRandomTests(4).Build();

        var chapter = new Chapter
        {
            Id = 10,
            Code = "ch01",
            Description = "Chapter 1",
            Assignments = [untouchedAssignment, inProgressAssignment1, inProgressAssignment2]
        };

        var passedTestIds = new HashSet<int>
        {
            inProgressAssignment1.Tests.First().Id,
            inProgressAssignment2.Tests.First().Id
        };

        // When requesting 2 recommendations, both in-progress should be selected even though untouched was first in the collection
        var recommendations = AssignmentRecommendationPolicy.RecommendAssignments([chapter], passedTestIds, 2);

        Assert.That(recommendations, Has.Count.EqualTo(2));
        Assert.That(recommendations[0].AssignmentId, Is.EqualTo(inProgressAssignment1.Id));
        Assert.That(recommendations[1].AssignmentId, Is.EqualTo(inProgressAssignment2.Id));

        // When requesting 3 recommendations, untouched should be included third
        var recs3 = AssignmentRecommendationPolicy.RecommendAssignments([chapter], passedTestIds, 3);
        Assert.That(recs3, Has.Count.EqualTo(3));
        Assert.That(recs3[2].AssignmentId, Is.EqualTo(untouchedAssignment.Id));
        Assert.That(recs3[2].GreenTestsCount, Is.EqualTo(0));
    }

    [Test]
    public void RecommendAssignments_NullPassedTestIds_TreatsAllAsUntouched()
    {
        var assignment1 = new AssignmentBuilder().WithId(1).WithCode("a1").WithRandomTests(2).Build();
        var assignment2 = new AssignmentBuilder().WithId(2).WithCode("a2").WithRandomTests(2).Build();

        var chapter = new Chapter
        {
            Id = 10,
            Code = "ch01",
            Description = "Chapter 1",
            Assignments = [assignment1, assignment2]
        };

        var recommendations = AssignmentRecommendationPolicy.RecommendAssignments([chapter], null, 2);

        Assert.That(recommendations, Has.Count.EqualTo(2));
        Assert.That(recommendations[0].GreenTestsCount, Is.EqualTo(0));
        Assert.That(recommendations[1].GreenTestsCount, Is.EqualTo(0));
    }
}

