using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Application.Repositories;

public interface IBasicRepository<T> where T : IEntity
{
    Task<T> GetByIdAsync(params object[] idValues);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> AddAsync(T newEntity);
    Task<T> UpdateAsync(T existingEntity);
    Task DeleteAsync(T entityToDelete);
    Task DeleteByIdAsync(params object[] idValues);
    Task DeleteBulkAsync(IEnumerable<T> entitiesToDelete);
}