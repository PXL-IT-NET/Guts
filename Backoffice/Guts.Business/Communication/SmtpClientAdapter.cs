using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Guts.Business.Communication
{
    internal class SmtpClientAdapter : ISmtpClient
    {
        private readonly SmtpClient _smtpClient;

        public SmtpClientAdapter(string smtpHost, int port, string username, string password)
        {
            _smtpClient = new SmtpClient(smtpHost)
            {
                Port = port,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password),
                Timeout = 7 * 1000 // 7 seconds
            };
        }

        public async Task SendMailAsync(MailMessage message)
        {
            await _smtpClient.SendMailAsync(message);
        }
    }
}