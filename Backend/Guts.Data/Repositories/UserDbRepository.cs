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

        public async Task<IList<User>> GetUsersOfTopicAsync(int topicId)
        {
            var users = _context.Users.FromSql(
                "CALL sp_getUsersOfTopic({0})", topicId);

            return await users.AsNoTracking().ToListAsync();
        }
    }
}