using System.Linq;
using System.Threading.Tasks;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;

namespace Guts.Infrastructure.Repositories
{
    internal class ExamPartDbRepository : BaseDbRepository<IExamPart, ExamPart>, IExamPartRepository
    {
        public ExamPartDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<IExamPart> LoadWithAssignmentEvaluationsAsync(int examPartId)
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

        public override async Task DeleteAsync(IExamPart entityToDelete)
        {
            var entry = _context.Entry(entityToDelete);
            await entry.Collection(ep => ep.AssignmentEvaluations).LoadAsync();
            _context.Set<AssignmentEvaluation>().RemoveRange(entityToDelete.AssignmentEvaluations.OfType<AssignmentEvaluation>());
            await base.DeleteAsync(entityToDelete);
        }
    }
}