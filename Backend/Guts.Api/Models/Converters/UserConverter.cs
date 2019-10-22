using Guts.Domain.UserAggregate;

namespace Guts.Api.Models.Converters
{
    public class UserConverter : IUserConverter
    {
        public UserModel FromUser(User user)
        {
            return new UserModel
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}".Trim()
            };
        }
    }
}