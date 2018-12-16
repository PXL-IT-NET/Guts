using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Data.Repositories
{
    public interface IUserRepository
    {
        Task<IList<User>> GetUsersOfChapterAsync(int chapterId);
    }
}