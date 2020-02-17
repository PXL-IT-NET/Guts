using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain.ValueObjects;

namespace Guts.Business.Repositories
{
    public interface ISolutionFileRepository
    {
        Task<SolutionFile> GetLatestForUserAsync(int assignmentId, int userId, FilePath filePath);

        Task<IList<SolutionFile>> GetAllLatestOfAssignmentAsync(int assignmentId);

        Task<IList<SolutionFile>> GetAllLatestOfAssignmentForUserAsync(int assignmentId, int userId, DateTime? dateUtc);

        Task<IList<SolutionFile>> GetAllLatestOfAssignmentForTeamAsync(int assignmentId, int teamId, DateTime? dateUtc);

        Task<SolutionFile> AddAsync(SolutionFile newSolutionFile);
    }
}