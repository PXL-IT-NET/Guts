using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Repositories;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class TestRunDbRepository : BaseDbRepository<TestRun, TestRun>, ITestRunRepository
{
    public TestRunDbRepository(GutsContext context) : base(context)
    {
    }

    public async Task<IList<TestRun>> GetUserTestRunsForAssignmentAsync(int assignmentId, int userId, DateTime? date)
    {
        var query = from assignment in _context.Assignments
            from testrun in assignment.TestRuns
            where (assignment.Id == assignmentId) &&
                  (testrun.UserId == userId) &&
                  (date == null || testrun.CreateDateTime <= date)
            orderby testrun.CreateDateTime
            select testrun;

        return await query.ToListAsync();
    }

    public async Task<IList<TestRun>> GetLastTestRunForAssignmentOfAllUsersAsync(int assignmentId)
    {
        var lastTestRunsDataQuery = from assignment in _context.Assignments
            from testrun in assignment.TestRuns
            where assignment.Id == assignmentId
            orderby testrun.CreateDateTime descending
            group testrun by new {testrun.AssignmentId, testrun.UserId} into testRunGroups
            select new
            {
                testRunGroups.Key.AssignmentId,
                testRunGroups.Key.UserId,
                CreateDateTime = testRunGroups.Max(tr => tr.CreateDateTime)
            };

        var lastTestRunsQuery = from testrun in _context.TestRuns
            from testrunData in lastTestRunsDataQuery
            where testrun.AssignmentId == testrunData.AssignmentId
                  && testrun.UserId == testrunData.UserId
                  && testrun.CreateDateTime == testrunData.CreateDateTime
            select testrun;

        var lastTestRuns = await lastTestRunsQuery.Include(testrun => testrun.User).ToListAsync();
        return lastTestRuns;
    }

    public async Task RemoveAllTopicTestRunsOfUserAsync(int topicId, int userId)
    {
        List<TestRun> testRunsToDelete = await _context.TestRuns
            .Where(testRun => testRun.Assignment.TopicId == topicId && testRun.UserId == userId)
            .Include(testRun => testRun.TestResults).ToListAsync();

        foreach (TestRun testRun in testRunsToDelete)
        {
            _context.TestResults.RemoveRange(testRun.TestResults);
        }

        _context.TestRuns.RemoveRange(testRunsToDelete);

        var query = from assignment in _context.Assignments
            join solutionFile in _context.SolutionFiles on assignment.Id equals solutionFile.AssignmentId
            where assignment.TopicId == topicId && solutionFile.UserId == userId
            select solutionFile;
        List<SolutionFile> solutionFilesToDelete = await query.ToListAsync();

        _context.SolutionFiles.RemoveRange(solutionFilesToDelete);
            
        await _context.SaveChangesAsync();
    }

    public async Task<IList<TestRun>> GetTeamTestRunsForAssignmentAsync(int assignmentId, int teamId, DateTime? dateUtc)
    {
        var query = from assignment in _context.Assignments
            from testrun in assignment.TestRuns
            join teamUser in _context.ProjectTeamUsers on testrun.UserId equals teamUser.UserId
            where (assignment.Id == assignmentId) &&
                  (teamUser.ProjectTeamId == teamId) &&
                  (assignment.TopicId == teamUser.ProjectTeam.ProjectId) &&
                  (dateUtc == null || testrun.CreateDateTime <= dateUtc)
            orderby testrun.CreateDateTime
            select testrun;

        return await query.ToListAsync();
    }

    public async Task<IReadOnlyList<PruningAssignmentTarget>> GetPruningAssignmentTargetsForPeriodAsync(
        int periodId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Assignments
            .AsNoTracking()
            .Where(a => a.Topic.PeriodId == periodId
                        && !a.Topic.Period.IsDeleting
                        && !a.Topic.Course.IsDeleting
                        && a.TestRuns.Count > 1)
            .OrderBy(a => a.Id)
            .Select(a => new PruningAssignmentTarget(a.Id, a.TopicId, a.Topic is Project))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TestRunSnapshot>> GetTestRunSnapshotsForAssignmentAsync(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        var runs = await _context.TestRuns
            .AsNoTracking()
            .Where(r => r.AssignmentId == assignmentId)
            .Select(r => new { r.Id, r.UserId, r.CreateDateTime })
            .ToListAsync(cancellationToken);

        if (runs.Count <= 1)
        {
            return runs
                .Select(r => new TestRunSnapshot(r.Id, r.UserId, r.CreateDateTime, []))
                .ToList();
        }

        var results = await _context.TestResults
            .AsNoTracking()
            .Where(tr => tr.TestRun.AssignmentId == assignmentId)
            .Select(tr => new { tr.TestRunId, tr.TestId, tr.Passed })
            .ToListAsync(cancellationToken);

        ILookup<int, TestResultSnapshot> resultsByRunId = results.ToLookup(
            r => r.TestRunId,
            r => new TestResultSnapshot(r.TestId, r.Passed));

        return runs
            .Select(r => new TestRunSnapshot(
                r.Id,
                r.UserId,
                r.CreateDateTime,
                resultsByRunId[r.Id].ToList()))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<int, int>> GetProjectTeamMembershipMapAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await _context.ProjectTeamUsers
            .AsNoTracking()
            .Where(ptu => ptu.ProjectTeam.ProjectId == projectId)
            .Select(ptu => new { ptu.UserId, ptu.ProjectTeamId })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<int, int>(memberships.Count);
        foreach (var membership in memberships)
        {
            map.TryAdd(membership.UserId, membership.ProjectTeamId);
        }

        return map;
    }

    public async Task<TestRunBatchDeletionResult> DeleteTestRunsBatchAsync(
        IReadOnlyCollection<int> testRunIds,
        CancellationToken cancellationToken = default)
    {
        if (testRunIds == null || testRunIds.Count == 0)
        {
            return new TestRunBatchDeletionResult(0, 0);
        }

        int resultsDeleted = await _context.TestResults
            .Where(tr => testRunIds.Contains(tr.TestRunId))
            .ExecuteDeleteAsync(cancellationToken);

        int runsDeleted = await _context.TestRuns
            .Where(r => testRunIds.Contains(r.Id))
            .ExecuteDeleteAsync(cancellationToken);

        return new TestRunBatchDeletionResult(runsDeleted, resultsDeleted);
    }

    public async Task<IReadOnlyList<TestRunSnapshot>> GetUserTestRunSnapshotsForAssignmentAsync(
        int assignmentId,
        int userId,
        DateTime? dateUtc,
        CancellationToken cancellationToken = default)
    {
        var runs = await _context.TestRuns
            .AsNoTracking()
            .Where(r => r.AssignmentId == assignmentId
                        && r.UserId == userId
                        && (dateUtc == null || r.CreateDateTime <= dateUtc))
            .OrderBy(r => r.CreateDateTime)
            .ThenBy(r => r.Id)
            .Select(r => new { r.Id, r.UserId, r.CreateDateTime })
            .ToListAsync(cancellationToken);

        if (runs.Count == 0)
        {
            return [];
        }

        var results = await _context.TestResults
            .AsNoTracking()
            .Where(tr => tr.TestRun.AssignmentId == assignmentId
                         && tr.TestRun.UserId == userId
                         && (dateUtc == null || tr.TestRun.CreateDateTime <= dateUtc))
            .Select(tr => new { tr.TestRunId, tr.TestId, tr.Passed })
            .ToListAsync(cancellationToken);

        ILookup<int, TestResultSnapshot> resultsByRunId = results.ToLookup(
            r => r.TestRunId,
            r => new TestResultSnapshot(r.TestId, r.Passed));

        return runs
            .Select(r => new TestRunSnapshot(r.Id, r.UserId, r.CreateDateTime, resultsByRunId[r.Id].ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<TestRunSnapshot>> GetTeamTestRunSnapshotsForAssignmentAsync(
        int assignmentId,
        int teamId,
        DateTime? dateUtc,
        CancellationToken cancellationToken = default)
    {
        var runs = await (from testrun in _context.TestRuns.AsNoTracking()
            join teamUser in _context.ProjectTeamUsers.AsNoTracking() on testrun.UserId equals teamUser.UserId
            where testrun.AssignmentId == assignmentId
                  && teamUser.ProjectTeamId == teamId
                  && testrun.Assignment.TopicId == teamUser.ProjectTeam.ProjectId
                  && (dateUtc == null || testrun.CreateDateTime <= dateUtc)
            orderby testrun.CreateDateTime, testrun.Id
            select new { testrun.Id, testrun.UserId, testrun.CreateDateTime })
            .ToListAsync(cancellationToken);

        if (runs.Count == 0)
        {
            return [];
        }

        var results = await (from tr in _context.TestResults.AsNoTracking()
            join teamUser in _context.ProjectTeamUsers.AsNoTracking() on tr.TestRun.UserId equals teamUser.UserId
            where tr.TestRun.AssignmentId == assignmentId
                  && teamUser.ProjectTeamId == teamId
                  && tr.TestRun.Assignment.TopicId == teamUser.ProjectTeam.ProjectId
                  && (dateUtc == null || tr.TestRun.CreateDateTime <= dateUtc)
            select new { tr.TestRunId, tr.TestId, tr.Passed })
            .ToListAsync(cancellationToken);

        ILookup<int, TestResultSnapshot> resultsByRunId = results.ToLookup(
            r => r.TestRunId,
            r => new TestResultSnapshot(r.TestId, r.Passed));

        return runs
            .Select(r => new TestRunSnapshot(r.Id, r.UserId, r.CreateDateTime, resultsByRunId[r.Id].ToList()))
            .ToList();
    }
}
