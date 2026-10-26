using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories;

internal class SolutionFileDbRepository : ISolutionFileRepository
{
    private readonly GutsContext _context;

    public SolutionFileDbRepository(GutsContext context)
    {
        _context = context;
    }

    public async Task<SolutionFile> GetLatestForUserAsync(int assignmentId, int userId, FilePath filePath)
    {
        return await FilterByAssignmentAndDate(assignmentId)
            .Where(solutionFile => solutionFile.UserId == userId && solutionFile.FilePath == filePath)
            .OrderByDescending(solutionFile => solutionFile.ModifyDateTime)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<SolutionFile>> GetAllLatestOfAssignmentAsync(int assignmentId)
    {
        var filteredFiles = FilterByAssignmentAndDate(assignmentId);
        return await BuildLatestPerUserQuery(filteredFiles)
            .Include(file => file.User)
            .ToListAsync();
    }

    public async Task<IList<SolutionFile>> GetAllLatestOfAssignmentForUserAsync(int assignmentId, int userId, DateTime? dateUtc)
    {
        var filteredFiles = FilterByAssignmentAndDate(assignmentId, dateUtc)
            .Where(solutionFile => solutionFile.UserId == userId);

        return await BuildLatestPerUserQuery(filteredFiles).ToListAsync();
    }

    public async Task<IList<SolutionFile>> GetAllLatestOfAssignmentForTeamAsync(int assignmentId, int teamId, DateTime? dateUtc)
    {
        var teamFiles = FilterByTeam(FilterByAssignmentAndDate(assignmentId, dateUtc), teamId);

        var latestSolutionFilesDataQuery = teamFiles
            .OrderByDescending(solutionFile => solutionFile.ModifyDateTime)
            .GroupBy(solutionFile => new { solutionFile.AssignmentId, solutionFile.FilePath })
            .Select(fileGroups => new
            {
                fileGroups.Key.AssignmentId,
                fileGroups.Key.FilePath,
                ModifyDateTime = fileGroups.Max(file => file.ModifyDateTime)
            });

        var latestSolutionFilesQuery = from solutionFile in teamFiles
            from solutionFileData in latestSolutionFilesDataQuery
            where solutionFile.AssignmentId == solutionFileData.AssignmentId &&
                  solutionFile.FilePath == solutionFileData.FilePath &&
                  solutionFile.ModifyDateTime == solutionFileData.ModifyDateTime
            select solutionFile;

        return await latestSolutionFilesQuery.ToListAsync();
    }

    public async Task<SolutionFile> AddAsync(SolutionFile newSolutionFile)
    {
        var entry = await _context.SolutionFiles.AddAsync(newSolutionFile);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    private IQueryable<SolutionFile> FilterByAssignmentAndDate(int assignmentId, DateTime? dateUtc = null)
    {
        return _context.SolutionFiles.Where(solutionFile =>
            solutionFile.AssignmentId == assignmentId &&
            (dateUtc == null || solutionFile.ModifyDateTime <= dateUtc));
    }

    private IQueryable<SolutionFile> FilterByTeam(IQueryable<SolutionFile> solutionFiles, int teamId)
    {
        return from solutionFile in solutionFiles
            join teamUser in _context.ProjectTeamUsers on solutionFile.UserId equals teamUser.UserId
            where teamUser.ProjectTeamId == teamId
            select solutionFile;
    }

    private IQueryable<SolutionFile> BuildLatestPerUserQuery(IQueryable<SolutionFile> filteredFiles)
    {
        var latestSolutionFilesDataQuery = filteredFiles
            .OrderByDescending(solutionFile => solutionFile.ModifyDateTime)
            .GroupBy(solutionFile => new { solutionFile.AssignmentId, solutionFile.UserId, solutionFile.FilePath })
            .Select(fileGroups => new
            {
                fileGroups.Key.AssignmentId,
                fileGroups.Key.UserId,
                fileGroups.Key.FilePath,
                ModifyDateTime = fileGroups.Max(file => file.ModifyDateTime)
            });

        return from solutionFile in _context.SolutionFiles
            from solutionFileData in latestSolutionFilesDataQuery
            where solutionFile.AssignmentId == solutionFileData.AssignmentId &&
                  solutionFile.UserId == solutionFileData.UserId &&
                  solutionFile.FilePath == solutionFileData.FilePath &&
                  solutionFile.ModifyDateTime == solutionFileData.ModifyDateTime
            select solutionFile;
    }
}