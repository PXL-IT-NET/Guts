using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Guts.Data.Repositories
{
    public class LoginSesssionDbRepository : BaseDbRepository<LoginSession>, ILoginSesssionRepository
    {
        public LoginSesssionDbRepository(GutsContext context) : base(context)
        {
        }

        public async Task<LoginSession> GetSingleAsync(string publicIdentifier, string ipAddress = null, string sessionToken = null)
        {
            var session = await _context.LoginSessions.FirstOrDefaultAsync(s =>
                (s.PublicIdentifier == publicIdentifier) && 
                (ipAddress == null || s.IpAddress == ipAddress) &&
                (sessionToken == null || s.SessionToken == sessionToken));

            if (session == null)
            {
                throw new DataNotFoundException();
            }

            return session;
        }
    }
}