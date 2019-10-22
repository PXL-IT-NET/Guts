using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class ExamPartDbRepository : BaseDbRepository<ExamPart>, IExamPartRepository
    {
        public ExamPartDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<ExamPart> LoadWithAssignmentEvaluationsAsync(int examPartId)
        {
            var evaluation = await _context.ExamParts.Where(e => e.Id == examPartId)
                .Include(e => e.AssignmentEvaluations)
                .FirstOrDefaultAsync();

            if (evaluation == null)
            {
                throw new DataNotFoundException();
            }

            return evaluation;
        }
    }
}