using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IBasicRepository<T> where T : class, IDomainObject
    {
        Task<T> GetByIdAsync(int id);
        Task<IList<T>> GetAllAsync();
        Task<T> AddAsync(T newEntity);
        Task<T> UpdateAsync(T existingEntity);
        Task DeleteBulkAsync(IEnumerable<T> entitiesToDelete);
    }
}