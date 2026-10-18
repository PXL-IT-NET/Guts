using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Guts.Domain.UserAggregate;

namespace Guts.Business.Communication
{
    internal class MailSender : IMailSender
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

        public Task SendConfirmUserEmailMessageAsync(User user, string confirmationToken)
        {
            return SendUserTokenMailAsync(
                user,
                subject: "Please confirm your registration",
                callbackPath: "confirmemail",
                token: confirmationToken,
                messageIntro: "Please confirm your registration for the GUTS project of the college university PXL.",
                ignoreCondition: "register");
        }

        public Task SendForgotPasswordMessageAsync(User user, string forgotPasswordToken)
        {
            return SendUserTokenMailAsync(
                user,
                subject: "Reset password",
                callbackPath: "resetpassword",
                token: forgotPasswordToken,
                messageIntro: "A request to reset your password has been made.",
                ignoreCondition: "indicated that you forgot your password");
        }

        private async Task SendUserTokenMailAsync(
            User user,
            string subject,
            string callbackPath,
            string token,
            string messageIntro,
            string ignoreCondition)
        {
            var callbackUri = new Uri(_webAppBaseUri, $"{callbackPath}?userId={user.Id}&token={HttpUtility.UrlEncode(token)}");

            var bodyBuilder = new StringBuilder();
            bodyBuilder.AppendLine("<html><body>");
            bodyBuilder.AppendLine("<p>");
            bodyBuilder.AppendLine("Dear student,<br/><br/>");
            bodyBuilder.AppendLine($"{messageIntro}<br/>");
            bodyBuilder.AppendLine("You can do this by following the link below:<br/><br/>");
            bodyBuilder.AppendLine($"<a href=\"{callbackUri.AbsoluteUri}\">{callbackUri.AbsoluteUri}</a><br/><br/>");
            bodyBuilder.AppendLine($"If you did not {ignoreCondition} via {_webAppBaseUri.AbsoluteUri}, then you can just ignore this email.<br/>");
            bodyBuilder.AppendLine("</p>");
            bodyBuilder.AppendLine("</body></html>");

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = subject,
                Body = bodyBuilder.ToString(),
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(user.Email));

            await _smtpClient.SendMailAsync(message);
        }
    }
}