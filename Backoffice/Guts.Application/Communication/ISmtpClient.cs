using System.Net.Mail;
using System.Threading.Tasks;

namespace Guts.Application.Communication;

public interface ISmtpClient
{
    Task SendMailAsync(MailMessage message);
}