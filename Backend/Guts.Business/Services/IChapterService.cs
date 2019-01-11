using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Services
{
    public interface IChapterService
    {
        Task<Chapter> GetOrCreateChapterAsync(string courseCode, string chapterCode);
        Task<Chapter> LoadChapterAsync(int courseId, string chapterCode);
        Task<Chapter> LoadChapterWithTestsAsync(int courseId, string chapterCode);
        Task<IList<AssignmentResultDto>> GetResultsForUserAsync(Chapter chapter, int userId, DateTime? dateUtc);
        Task<IList<AssignmentStatisticsDto>> GetChapterStatisticsAsync(Chapter chapter, DateTime? dateUtc);
        Task<IList<Chapter>> GetChaptersOfCourseAsync(int courseId);
    }
}