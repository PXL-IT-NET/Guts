using System.Net.Mail;
using System.Threading.Tasks;

namespace Guts.Business.Communication
{
    public interface ISmtpClient
    {
        Task SendMailAsync(MailMessage message);
    }
}