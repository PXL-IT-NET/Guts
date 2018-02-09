using System.Threading.Tasks;
using Guts.Domain;

namespace Guts.Business.Communication
{
    public interface IMailSender
    {
        Task SendConfirmUserEmailMessageAsync(User user, string confirmationToken);
        Task SendForgotPasswordMessageAsync(User user, string forgotPasswordToken);
    }
}
