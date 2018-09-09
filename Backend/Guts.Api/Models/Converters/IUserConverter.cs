using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IUserConverter
    {
        UserModel FromUser(User user);
    }
}