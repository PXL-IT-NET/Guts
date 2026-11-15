using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guts.Application.Repositories;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.DeletionJobAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class ChunkedDeletionDbRepository(GutsContext context)
    : BaseDbRepository<DeletionJob, DeletionJob>(context), IChunkedDeletionRepository
{
    private const int IdSliceSize = 500;

    private sealed record DeletionScope(
        int[] TopicIds,
        int[] AssignmentIds,
        int[] ProjectTeamIds,
        int[] ProjectAssessmentIds,
        int[] ProjectTeamAssessmentIds,
        int[] StudentCourseProfileIds);

    public async Task<long> CountDependentAndRootRowsAsync(
        DeletionTargetType targetType,
        int targetId,
        CancellationToken cancellationToken = default)
    {
        DeletionScope scope = await LoadScopeAsync(targetType, targetId, cancellationToken);
        long total = 0;

        total += await CountBySlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.TestResults
                .Where(tr => _context.TestRuns.Any(r => r.Id == tr.TestRunId && slice.Contains(r.AssignmentId)) ||
                             _context.Tests.Any(t => t.Id == tr.TestId && slice.Contains(t.AssignmentId)))
                .LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.SolutionFiles.Where(sf => slice.Contains(sf.AssignmentId)).LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.ProjectTeamAssessmentIds,
            (slice, ct) => _context.PeerAssessments.Where(pa => slice.Contains(pa.ProjectTeamAssessmentId)).LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.ProjectTeamAssessmentIds,
            (slice, ct) => _context.Set<Dictionary<string, object>>("ProjectTeamAssessmentPeers")
                .Where(p => slice.Contains(EF.Property<int>(p, "ProjectTeamAssessmentId")))
                .LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.StudentCourseProfileIds,
            (slice, ct) => _context.GamificationEvents.Where(ge => slice.Contains(ge.StudentCourseProfileId)).LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.StudentCourseProfileIds,
            (slice, ct) => _context.StudentBadges.Where(sb => slice.Contains(sb.StudentCourseProfileId)).LongCountAsync(ct),
            cancellationToken);

        total += await CountBySlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.TestRuns.Where(tr => slice.Contains(tr.AssignmentId)).LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.Tests.Where(t => slice.Contains(t.AssignmentId)).LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.Set<TestCodeHash>().Where(h => slice.Contains(h.AssignmentId)).LongCountAsync(ct),
            cancellationToken);
        total += await CountBySlicesAsync(scope.ProjectTeamAssessmentIds,
            (slice, ct) => _context.ProjectTeamAssessments.Where(pta => slice.Contains(pta.Id)).LongCountAsync(ct),
            cancellationToken);

        total += scope.ProjectAssessmentIds.Length;
        total += await CountBySlicesAsync(scope.ProjectTeamIds,
            (slice, ct) => _context.ProjectTeamUsers.Where(ptu => slice.Contains(ptu.ProjectTeamId)).LongCountAsync(ct),
            cancellationToken);
        total += scope.ProjectTeamIds.Length;
        total += scope.AssignmentIds.Length;
        total += scope.StudentCourseProfileIds.Length;
        total += await BuildCourseClassGroupQuery(targetType, targetId).LongCountAsync(cancellationToken);
        total += scope.TopicIds.Length;

        if (targetType == DeletionTargetType.Course)
        {
            total += await _context.CourseGamificationConfigs.Where(c => c.CourseId == targetId).LongCountAsync(cancellationToken);
            total += await _context.Courses.Where(c => c.Id == targetId).LongCountAsync(cancellationToken);
        }
        else
        {
            total += await _context.Periods.Where(p => p.Id == targetId).LongCountAsync(cancellationToken);
        }

        return total;
    }

    public async Task ExecuteChunkedDeletionAsync(
        DeletionTargetType targetType,
        int targetId,
        int batchSize,
        Func<int, string, Task> onBatchDeletedAsync,
        CancellationToken cancellationToken = default)
    {
        DeletionScope scope = await LoadScopeAsync(targetType, targetId, cancellationToken);

        // Leaf level 1
        await DeleteByIdSlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.TestResults
                .Where(tr => _context.TestRuns.Any(r => r.Id == tr.TestRunId && slice.Contains(r.AssignmentId)) ||
                             _context.Tests.Any(t => t.Id == tr.TestId && slice.Contains(t.AssignmentId)))
                .OrderBy(tr => tr.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting test results", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.SolutionFiles
                .Where(sf => slice.Contains(sf.AssignmentId))
                .OrderBy(sf => sf.AssignmentId)
                .ThenBy(sf => sf.UserId)
                .ThenBy(sf => sf.ModifyDateTime)
                .ThenBy(sf => sf.FilePath)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting solution files", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.ProjectTeamAssessmentIds,
            (slice, ct) => _context.PeerAssessments
                .Where(pa => slice.Contains(pa.ProjectTeamAssessmentId))
                .OrderBy(pa => pa.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting peer assessments", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.ProjectTeamAssessmentIds,
            (slice, ct) => _context.Set<Dictionary<string, object>>("ProjectTeamAssessmentPeers")
                .Where(p => slice.Contains(EF.Property<int>(p, "ProjectTeamAssessmentId")))
                .OrderBy(p => EF.Property<int>(p, "ProjectTeamAssessmentId"))
                .ThenBy(p => EF.Property<int>(p, "UserId"))
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting assessment peer links", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.StudentCourseProfileIds,
            (slice, ct) => _context.GamificationEvents
                .Where(ge => slice.Contains(ge.StudentCourseProfileId))
                .OrderBy(ge => ge.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting gamification events", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.StudentCourseProfileIds,
            (slice, ct) => _context.StudentBadges
                .Where(sb => slice.Contains(sb.StudentCourseProfileId))
                .OrderBy(sb => sb.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting student badges", onBatchDeletedAsync, cancellationToken);

        // Level 2
        await DeleteByIdSlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.TestRuns
                .Where(tr => slice.Contains(tr.AssignmentId))
                .OrderBy(tr => tr.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting test runs", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.Tests
                .Where(t => slice.Contains(t.AssignmentId))
                .OrderBy(t => t.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting tests", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.Set<TestCodeHash>()
                .Where(h => slice.Contains(h.AssignmentId))
                .OrderBy(h => h.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting test code hashes", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.ProjectTeamAssessmentIds,
            (slice, ct) => _context.ProjectTeamAssessments
                .Where(pta => slice.Contains(pta.Id))
                .OrderBy(pta => pta.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting project team assessments", onBatchDeletedAsync, cancellationToken);

        // Level 3
        await DeleteByIdSlicesAsync(scope.ProjectAssessmentIds,
            (slice, ct) => _context.Set<ProjectAssessment>()
                .Where(pa => slice.Contains(pa.Id))
                .OrderBy(pa => pa.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting project assessments", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.ProjectTeamIds,
            (slice, ct) => _context.ProjectTeamUsers
                .Where(ptu => slice.Contains(ptu.ProjectTeamId))
                .OrderBy(ptu => ptu.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting project team memberships", onBatchDeletedAsync, cancellationToken);

        // Level 4
        await DeleteByIdSlicesAsync(scope.ProjectTeamIds,
            (slice, ct) => _context.ProjectTeams
                .Where(pt => slice.Contains(pt.Id))
                .OrderBy(pt => pt.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting project teams", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.AssignmentIds,
            (slice, ct) => _context.Assignments
                .Where(a => slice.Contains(a.Id))
                .OrderBy(a => a.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting assignments", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.StudentCourseProfileIds,
            (slice, ct) => _context.StudentCourseProfiles
                .Where(sp => slice.Contains(sp.Id))
                .OrderBy(sp => sp.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting student course profiles", onBatchDeletedAsync, cancellationToken);

        // Level 5
        await DeleteBatchesAsync(
            ct => BuildCourseClassGroupQuery(targetType, targetId)
                .OrderBy(cg => cg.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting course class groups", onBatchDeletedAsync, cancellationToken);

        await DeleteByIdSlicesAsync(scope.TopicIds,
            (slice, ct) => _context.Topics
                .Where(t => slice.Contains(t.Id))
                .OrderBy(t => t.Id)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct),
            "Deleting chapters and projects", onBatchDeletedAsync, cancellationToken);

        // Level 6 (Root & course-level config)
        if (targetType == DeletionTargetType.Course)
        {
            await DeleteBatchesAsync(
                ct => _context.CourseGamificationConfigs
                    .Where(gc => gc.CourseId == targetId)
                    .OrderBy(gc => gc.Id)
                    .Take(batchSize)
                    .ExecuteDeleteAsync(ct),
                "Deleting course gamification config", onBatchDeletedAsync, cancellationToken);

            await DeleteBatchesAsync(
                ct => _context.Courses.Where(c => c.Id == targetId).ExecuteDeleteAsync(ct),
                "Removing course", onBatchDeletedAsync, cancellationToken);
        }
        else
        {
            await DeleteBatchesAsync(
                ct => _context.Periods.Where(p => p.Id == targetId).ExecuteDeleteAsync(ct),
                "Removing period", onBatchDeletedAsync, cancellationToken);
        }
    }

    private async Task<DeletionScope> LoadScopeAsync(
        DeletionTargetType targetType,
        int targetId,
        CancellationToken cancellationToken)
    {
        IQueryable<Topic> topicQuery = targetType == DeletionTargetType.Course
            ? _context.Topics.Where(t => t.CourseId == targetId)
            : _context.Topics.Where(t => t.PeriodId == targetId);

        int[] topicIds = await topicQuery.Select(t => t.Id).ToArrayAsync(cancellationToken);

        int[] assignmentIds = await CollectIdsBySlicesAsync(
            topicIds,
            slice => _context.Assignments.Where(a => slice.Contains(a.TopicId)).Select(a => a.Id),
            cancellationToken);

        int[] projectTeamIds = await CollectIdsBySlicesAsync(
            topicIds,
            slice => _context.ProjectTeams.Where(pt => slice.Contains(pt.ProjectId)).Select(pt => pt.Id),
            cancellationToken);

        int[] projectAssessmentIds = await CollectIdsBySlicesAsync(
            topicIds,
            slice => _context.Set<ProjectAssessment>().Where(pa => slice.Contains(pa.ProjectId)).Select(pa => pa.Id),
            cancellationToken);

        int[] teamAssessmentsFromAssessments = await CollectIdsBySlicesAsync(
            projectAssessmentIds,
            slice => _context.ProjectTeamAssessments
                .Where(pta => slice.Contains(EF.Property<int>(pta, "ProjectAssessmentId")))
                .Select(pta => pta.Id),
            cancellationToken);

        int[] teamAssessmentsFromTeams = await CollectIdsBySlicesAsync(
            projectTeamIds,
            slice => _context.ProjectTeamAssessments
                .Where(pta => slice.Contains(EF.Property<int>(pta, "ProjectTeamId")))
                .Select(pta => pta.Id),
            cancellationToken);

        int[] projectTeamAssessmentIds = teamAssessmentsFromAssessments
            .Concat(teamAssessmentsFromTeams)
            .Distinct()
            .ToArray();

        int[] studentCourseProfileIds = await (targetType == DeletionTargetType.Course
                ? _context.StudentCourseProfiles.Where(sp => sp.CourseId == targetId)
                : _context.StudentCourseProfiles.Where(sp => sp.PeriodId == targetId))
            .Select(sp => sp.Id)
            .ToArrayAsync(cancellationToken);

        return new DeletionScope(
            topicIds,
            assignmentIds,
            projectTeamIds,
            projectAssessmentIds,
            projectTeamAssessmentIds,
            studentCourseProfileIds);
    }

    private IQueryable<Domain.GamificationAggregate.Configuration.CourseClassGroup> BuildCourseClassGroupQuery(
        DeletionTargetType targetType,
        int targetId)
    {
        return targetType == DeletionTargetType.Course
            ? _context.CourseClassGroups.Where(cg => cg.CourseId == targetId)
            : _context.CourseClassGroups.Where(cg => cg.PeriodId == targetId);
    }

    private static async Task<int[]> CollectIdsBySlicesAsync(
        int[] parentIds,
        Func<int[], IQueryable<int>> queryBuilder,
        CancellationToken cancellationToken)
    {
        if (parentIds.Length == 0)
        {
            return [];
        }

        List<int> result = [];
        foreach (int[] slice in parentIds.Chunk(IdSliceSize))
        {
            List<int> partial = await queryBuilder(slice).ToListAsync(cancellationToken);
            result.AddRange(partial);
        }

        return result.ToArray();
    }

    private static async Task<long> CountBySlicesAsync(
        int[] parentIds,
        Func<int[], CancellationToken, Task<long>> countSliceAsync,
        CancellationToken cancellationToken)
    {
        if (parentIds.Length == 0)
        {
            return 0;
        }

        long total = 0;
        foreach (int[] slice in parentIds.Chunk(IdSliceSize))
        {
            total += await countSliceAsync(slice, cancellationToken);
        }

        return total;
    }

    private async Task DeleteByIdSlicesAsync(
        int[] parentIds,
        Func<int[], CancellationToken, Task<int>> deleteSliceBatchAsync,
        string phase,
        Func<int, string, Task> onBatchDeletedAsync,
        CancellationToken cancellationToken)
    {
        if (parentIds.Length == 0)
        {
            return;
        }

        foreach (int[] slice in parentIds.Chunk(IdSliceSize))
        {
            await DeleteBatchesAsync(
                ct => deleteSliceBatchAsync(slice, ct),
                phase,
                onBatchDeletedAsync,
                cancellationToken);
        }
    }

    private async Task DeleteBatchesAsync(
        Func<CancellationToken, Task<int>> deleteBatchOperation,
        string phase,
        Func<int, string, Task> onBatchDeletedAsync,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int deleted = await ExecuteInTransactionAsync(() => deleteBatchOperation(cancellationToken));

            if (deleted <= 0)
            {
                break;
            }

            if (onBatchDeletedAsync != null)
            {
                await onBatchDeletedAsync(deleted, phase);
            }
        }
    }
}
