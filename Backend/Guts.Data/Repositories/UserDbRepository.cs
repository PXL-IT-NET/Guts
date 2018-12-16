using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class UserDbRepository :  IUserRepository
    {
        private readonly GutsContext _context;

        public UserDbRepository(GutsContext context)
        {
            _context = context;
        }

        public async Task<IList<User>> GetUsersOfChapterAsync(int chapterId)
        {
            var users = _context.Users.FromSql(
                "CALL sp_getUsersOfChapter({0})", chapterId);

            return await users.AsNoTracking().ToListAsync();
        }
    }
}