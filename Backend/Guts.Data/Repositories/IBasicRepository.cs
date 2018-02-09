using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IBasicRepository<T> where T : class, IDomainObject
    {
        Task<T> AddAsync(T newEntity);
        Task<T> GetByIdAsync(int id);
    }
}