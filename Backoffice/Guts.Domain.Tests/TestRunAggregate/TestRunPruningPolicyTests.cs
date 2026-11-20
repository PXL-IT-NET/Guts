using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain.TestRunAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.TestRunAggregate;

[TestFixture]
public class TestRunPruningPolicyTests
{
    private static readonly DateTime BaseTime = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void FindRedundantTestRunIds_WhenNullEmptyOrSingleRun_ShouldReturnEmpty()
    {
        Assert.That(TestRunPruningPolicy.FindRedundantTestRunIds(null), Is.Empty);
        Assert.That(TestRunPruningPolicy.FindRedundantTestRunIds([]), Is.Empty);

        var single = new[]
        {
            new TestRunSnapshot(1, 10, BaseTime, [new TestResultSnapshot(100, true)])
        };
        Assert.That(TestRunPruningPolicy.FindRedundantTestRunIds(single), Is.Empty);
    }

    [Test]
    public void FindRedundantTestRunIds_ConsecutiveIdenticalFullRuns_ShouldKeepOldestAndPruneNewer()
    {
        var runs = new[]
        {
            // Deliberately out of chronological order
            new TestRunSnapshot(3, 10, BaseTime.AddMinutes(10), [new TestResultSnapshot(100, true), new TestResultSnapshot(101, false)]),
            new TestRunSnapshot(1, 10, BaseTime, [new TestResultSnapshot(100, true), new TestResultSnapshot(101, false)]),
            new TestRunSnapshot(2, 10, BaseTime.AddMinutes(5), [new TestResultSnapshot(100, true), new TestResultSnapshot(101, false)])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs);

        Assert.That(redundant, Is.EquivalentTo(new[] { 2, 3 }));
    }

    [Test]
    public void FindRedundantTestRunIds_EqualTimestamps_ShouldBreakTieByTestRunIdAndKeepLowestId()
    {
        var runs = new[]
        {
            new TestRunSnapshot(20, 10, BaseTime, [new TestResultSnapshot(100, true)]),
            new TestRunSnapshot(10, 10, BaseTime, [new TestResultSnapshot(100, true)])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs);

        Assert.That(redundant, Is.EquivalentTo(new[] { 20 }));
    }

    [Test]
    public void FindRedundantTestRunIds_PartialSingleTestRuns_ShouldUseCumulativeState()
    {
        var runs = new[]
        {
            // Run 1: T1=Pass, T2=Fail (kept)
            new TestRunSnapshot(1, 10, BaseTime, [new TestResultSnapshot(1, true), new TestResultSnapshot(2, false)]),
            // Run 2: Partial run T2=Fail -> unchanged cumulative state (pruned)
            new TestRunSnapshot(2, 10, BaseTime.AddMinutes(2), [new TestResultSnapshot(2, false)]),
            // Run 3: Partial run T2=Pass -> flips T2 to green (kept)
            new TestRunSnapshot(3, 10, BaseTime.AddMinutes(4), [new TestResultSnapshot(2, true)]),
            // Run 4: Full run T1=Pass, T2=Pass -> matches cumulative state (pruned)
            new TestRunSnapshot(4, 10, BaseTime.AddMinutes(6), [new TestResultSnapshot(1, true), new TestResultSnapshot(2, true)]),
            // Run 5: Partial run introduces new TestId 3 -> kept
            new TestRunSnapshot(5, 10, BaseTime.AddMinutes(8), [new TestResultSnapshot(3, false)]),
            // Run 6: Partial run T3=False -> unchanged (pruned)
            new TestRunSnapshot(6, 10, BaseTime.AddMinutes(10), [new TestResultSnapshot(3, false)])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs);

        Assert.That(redundant, Is.EquivalentTo(new[] { 2, 4, 6 }));

        // Idempotency check: re-running after deleting redundant IDs yields 0 redundant runs
        var remaining = runs.Where(r => !redundant.Contains(r.TestRunId)).ToList();
        Assert.That(TestRunPruningPolicy.FindRedundantTestRunIds(remaining), Is.Empty);
    }

    [Test]
    public void FindRedundantTestRunIds_StateOscillation_ShouldKeepAllStateTransitions()
    {
        var runs = new[]
        {
            new TestRunSnapshot(1, 10, BaseTime, [new TestResultSnapshot(1, false)]),
            new TestRunSnapshot(2, 10, BaseTime.AddMinutes(1), [new TestResultSnapshot(1, true)]),
            new TestRunSnapshot(3, 10, BaseTime.AddMinutes(2), [new TestResultSnapshot(1, false)]),
            new TestRunSnapshot(4, 10, BaseTime.AddMinutes(3), [new TestResultSnapshot(1, false)])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs);

        Assert.That(redundant, Is.EquivalentTo(new[] { 4 }));
    }

    [Test]
    public void FindRedundantTestRunIds_EmptyRuns_ShouldKeepFirstEmptyRunAndPruneSubsequentEmptyRuns()
    {
        var runs = new[]
        {
            new TestRunSnapshot(1, 10, BaseTime, []),
            new TestRunSnapshot(2, 10, BaseTime.AddMinutes(1), null),
            new TestRunSnapshot(3, 10, BaseTime.AddMinutes(2), [new TestResultSnapshot(1, true)]),
            new TestRunSnapshot(4, 10, BaseTime.AddMinutes(3), [])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs);

        Assert.That(redundant, Is.EquivalentTo(new[] { 2, 4 }));
    }

    [Test]
    public void FindRedundantTestRunIds_DuplicateTestIdsInSameRun_ShouldFoldDeterministically()
    {
        var runs = new[]
        {
            new TestRunSnapshot(1, 10, BaseTime, [new TestResultSnapshot(1, false), new TestResultSnapshot(1, true)]),
            new TestRunSnapshot(2, 10, BaseTime.AddMinutes(1), [new TestResultSnapshot(1, true)])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs);

        Assert.That(redundant, Is.EquivalentTo(new[] { 2 }));
    }

    [Test]
    public void FindRedundantTestRunIds_DifferentUsersInChapter_ShouldPartitionIndependently()
    {
        var runs = new[]
        {
            new TestRunSnapshot(1, 10, BaseTime, [new TestResultSnapshot(1, true)]),
            new TestRunSnapshot(2, 20, BaseTime.AddMinutes(1), [new TestResultSnapshot(1, true)]),
            new TestRunSnapshot(3, 10, BaseTime.AddMinutes(2), [new TestResultSnapshot(1, true)])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs);

        Assert.That(redundant, Is.EquivalentTo(new[] { 3 }));
    }

    [Test]
    public void FindRedundantTestRunIds_ProjectWithTeams_ShouldEvaluateTeammatesTogetherAndUnassignedUsersSeparately()
    {
        // Team 100: Alice (user 1) & Bob (user 2)
        // Team 200: Charlie (user 3) & Diana (user 4)
        // Unassigned lector: user 99
        var teamMap = new Dictionary<int, int>
        {
            [1] = 100,
            [2] = 100,
            [3] = 200,
            [4] = 200
        };

        var runs = new[]
        {
            // Team 100 interleaving: Alice Pass -> Bob Fail -> Alice Pass => ALL 3 MUST BE KEPT!
            new TestRunSnapshot(1, 1, BaseTime, [new TestResultSnapshot(50, true)]),
            new TestRunSnapshot(2, 2, BaseTime.AddMinutes(5), [new TestResultSnapshot(50, false)]),
            new TestRunSnapshot(3, 1, BaseTime.AddMinutes(10), [new TestResultSnapshot(50, true)]),
            // Team 100: Bob repeats Pass -> unchanged team state => PRUNED
            new TestRunSnapshot(4, 2, BaseTime.AddMinutes(15), [new TestResultSnapshot(50, true)]),

            // Team 200: Charlie Pass -> Diana Pass => Diana's run PRUNED
            new TestRunSnapshot(5, 3, BaseTime, [new TestResultSnapshot(50, true)]),
            new TestRunSnapshot(6, 4, BaseTime.AddMinutes(5), [new TestResultSnapshot(50, true)]),

            // Unassigned lector (user 99): evaluated independently from teams
            new TestRunSnapshot(7, 99, BaseTime, [new TestResultSnapshot(50, true)]),
            new TestRunSnapshot(8, 99, BaseTime.AddMinutes(5), [new TestResultSnapshot(50, true)])
        };

        var redundant = TestRunPruningPolicy.FindRedundantTestRunIds(runs, teamMap);

        Assert.That(redundant, Is.EquivalentTo(new[] { 4, 6, 8 }));
    }
}
