using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Guts.Domain.UserAggregate;

namespace Guts.Business.Communication
{
    public class MailSender : IMailSender
    {
        private readonly Uri _webAppBaseUri;
        private readonly ISmtpClient _smtpClient;
        private readonly string _fromEmail;

        public MailSender(ISmtpClient smtpClient, string fromEmail, string webAppBaseUrl)
        {
            _webAppBaseUri = new Uri(webAppBaseUrl);
            _fromEmail = fromEmail;
            _smtpClient = smtpClient;
        }

        public async Task SendConfirmUserEmailMessageAsync(User user, string confirmationToken)
        {
            var callbackUri = new Uri(_webAppBaseUri, $"confirmemail?userId={user.Id}&token={confirmationToken}");

            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine("<html><body>");
            bodyBuilder.AppendLine("<p>");
            bodyBuilder.AppendLine("Dear student,<br/><br/>");
            bodyBuilder.AppendLine("Please confirm your registration for the GUTS project of the college university PXL.<br/>");
            bodyBuilder.AppendLine("You can do this by following the link below:<br/><br/>");
            bodyBuilder.AppendLine($"<a href=\"{callbackUri.AbsoluteUri}\">{callbackUri.AbsoluteUri}</a><br/><br/>");
            bodyBuilder.AppendLine($"If you did not register via {_webAppBaseUri.AbsoluteUri}, then you can just ignore this email.<br/>");
            bodyBuilder.AppendLine("</p>");
            bodyBuilder.AppendLine("</body></html>");

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = "Please confirm your registration",
                Body = bodyBuilder.ToString(),
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(user.Email));

            await _smtpClient.SendMailAsync(message);
        }

        public async Task SendForgotPasswordMessageAsync(User user, string forgotPasswordToken)
        {
            var callbackUri = new Uri(_webAppBaseUri, $"resetpassword?userId={user.Id}&token={HttpUtility.UrlEncode(forgotPasswordToken)}");

            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine("<html><body>");
            bodyBuilder.AppendLine("<p>");
            bodyBuilder.AppendLine("Dear student,<br/><br/>");
            bodyBuilder.AppendLine("A request to reset your password has been made.<br/>");
            bodyBuilder.AppendLine("You can do this by following the link below:<br/><br/>");
            bodyBuilder.AppendLine($"<a href=\"{callbackUri.AbsoluteUri}\">{callbackUri.AbsoluteUri}</a><br/><br/>");
            bodyBuilder.AppendLine($"If you did not indicated that you forgot your password via {_webAppBaseUri.AbsoluteUri}, then you can just ignore this email.<br/>");
            bodyBuilder.AppendLine("</p>");
            bodyBuilder.AppendLine("</body></html>");

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = "Reset password",
                Body = bodyBuilder.ToString(),
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(user.Email));

            await _smtpClient.SendMailAsync(message);
        }
    }
}