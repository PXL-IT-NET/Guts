using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Repositories
{
    public interface IBasicRepository<T> where T : Entity
    {
        Task<T> GetByIdAsync(int id);
        Task<IList<T>> GetAllAsync();
        Task<T> AddAsync(T newEntity);
        Task<T> UpdateAsync(T existingEntity);
        Task DeleteAsync(T entityToDelete);
        Task DeleteByIdAsync(int id);
        Task DeleteBulkAsync(IEnumerable<T> entitiesToDelete);
    }
}