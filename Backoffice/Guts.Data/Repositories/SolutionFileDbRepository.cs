using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Guts.Business.Repositories;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class SolutionFileDbRepository : ISolutionFileRepository
    {
        private readonly GutsContext _context;

        public SolutionFileDbRepository(GutsContext context)
        {
            _context = context;
        }

        public async Task<SolutionFile> GetLatestForUserAsync(int assignmentId, int userId, FilePath filePath)
        {
            return await (from solutionFile in _context.SolutionFiles
                          where solutionFile.AssignmentId == assignmentId &&
                                solutionFile.UserId == userId &&
                                solutionFile.FilePath == filePath
                          orderby solutionFile.ModifyDateTime descending
                          select solutionFile).FirstOrDefaultAsync();
        }

        public async Task<IList<SolutionFile>> GetAllLatestOfAssignmentAsync(int assignmentId)
        {
            var lastSolutionFilesDataQuery = from solutionFile in _context.SolutionFiles
                                             where solutionFile.AssignmentId == assignmentId
                                             orderby solutionFile.ModifyDateTime descending
                                             group solutionFile by new { solutionFile.AssignmentId, solutionFile.UserId, solutionFile.FilePath } into fileGroups
                                             select new
                                             {
                                                 fileGroups.Key.AssignmentId,
                                                 fileGroups.Key.UserId,
                                                 fileGroups.Key.FilePath,
                                                 ModifyDateTime = fileGroups.Max(file => file.ModifyDateTime)
                                             };

            var lastSolutionFilesQuery = from solutionFile in _context.SolutionFiles
                                         from solutionFileData in lastSolutionFilesDataQuery
                                         where solutionFile.AssignmentId == solutionFileData.AssignmentId &&
                                               solutionFile.UserId == solutionFileData.UserId &&
                                               solutionFile.FilePath == solutionFileData.FilePath &&
                                               solutionFile.ModifyDateTime == solutionFileData.ModifyDateTime
                                         select solutionFile;

            return await lastSolutionFilesQuery.Include(file => file.User).ToListAsync();
        }

        public async Task<IList<SolutionFile>> GetAllLatestOfAssignmentForUserAsync(int assignmentId, int userId, DateTime? dateUtc)
        {
            var lastSolutionFilesDataQuery = from solutionFile in _context.SolutionFiles
                                             where solutionFile.AssignmentId == assignmentId &&
                                                   solutionFile.UserId == userId &&
                                                   (dateUtc == null || solutionFile.ModifyDateTime <= dateUtc)
                                             orderby solutionFile.ModifyDateTime descending
                                             group solutionFile by new { solutionFile.AssignmentId, solutionFile.UserId, solutionFile.FilePath } into fileGroups
                                             select new
                                             {
                                                 fileGroups.Key.AssignmentId,
                                                 fileGroups.Key.UserId,
                                                 fileGroups.Key.FilePath,
                                                 ModifyDateTime = fileGroups.Max(file => file.ModifyDateTime)
                                             };

            var lastSolutionFilesQuery = from solutionFile in _context.SolutionFiles
                                         from solutionFileData in lastSolutionFilesDataQuery
                                         where solutionFile.AssignmentId == solutionFileData.AssignmentId &&
                                               solutionFile.UserId == solutionFileData.UserId &&
                                               solutionFile.FilePath == solutionFileData.FilePath &&
                                               solutionFile.ModifyDateTime == solutionFileData.ModifyDateTime
                                         select solutionFile;

            return await lastSolutionFilesQuery.ToListAsync();
        }

        public async Task<IList<SolutionFile>> GetAllLatestOfAssignmentForTeamAsync(int assignmentId, int teamId, DateTime? dateUtc)
        {
            var lastSolutionFilesDataQuery = from solutionFile in _context.SolutionFiles
                                             join teamUser in _context.ProjectTeamUsers on solutionFile.UserId equals teamUser.UserId
                                             where solutionFile.AssignmentId == assignmentId &&
                                                   teamUser.ProjectTeamId == teamId &&
                                                   (dateUtc == null || solutionFile.ModifyDateTime <= dateUtc)
                                             orderby solutionFile.ModifyDateTime descending
                                             group solutionFile by new { solutionFile.AssignmentId, solutionFile.FilePath } into fileGroups
                                             select new
                                             {
                                                 fileGroups.Key.AssignmentId,
                                                 fileGroups.Key.FilePath,
                                                 ModifyDateTime = fileGroups.Max(file => file.ModifyDateTime)
                                             };

            var lastSolutionFilesQuery = from solutionFile in _context.SolutionFiles
                                         join teamUser in _context.ProjectTeamUsers on solutionFile.UserId equals teamUser.UserId
                                         from solutionFileData in lastSolutionFilesDataQuery
                                         where teamUser.ProjectTeamId == teamId &&
                                               solutionFile.AssignmentId == solutionFileData.AssignmentId &&
                                               solutionFile.FilePath == solutionFileData.FilePath &&
                                               solutionFile.ModifyDateTime == solutionFileData.ModifyDateTime
                                         select solutionFile;

            return await lastSolutionFilesQuery.ToListAsync();
        }

        public async Task<SolutionFile> AddAsync(SolutionFile newSolutionFile)
        {
            var entry = await _context.SolutionFiles.AddAsync(newSolutionFile);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }
    }
}