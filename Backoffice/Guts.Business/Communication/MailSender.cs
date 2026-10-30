using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Guts.Business.Dtos.Gamification;
using Guts.Domain.UserAggregate;

namespace Guts.Business.Communication;

internal class MailSender(ISmtpClient smtpClient, string fromEmail, string webAppBaseUrl)
    : IMailSender
{
    private readonly Uri _webAppBaseUri = new(webAppBaseUrl);

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

    public async Task SendStreakReminderEmailAsync(
        User user,
        string courseName,
        int courseId,
        int currentStreak,
        string targetStudentName,
        IReadOnlyList<StreakReminderAssignmentDto> recommendedAssignments)
    {
        var subject = $"Keep your momentum going in {courseName}!";
        string greetingName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : "student";
        var gamificationUri = new Uri(_webAppBaseUri, $"courses/{courseId}/gamification");

        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine("<html><body>");
        bodyBuilder.AppendLine("<p>");
        bodyBuilder.AppendLine($"Dear {greetingName},<br/><br/>");
        bodyBuilder.AppendLine($"You are currently on a {currentStreak}-week streak in {courseName}! Keep your momentum going by earning XP before Sunday 23:59 to keep your streak alive.<br/><br/>");

        if (!string.IsNullOrWhiteSpace(targetStudentName))
        {
            bodyBuilder.AppendLine($"🎯 <strong>Target to beat:</strong> You're close to overtaking <strong>{targetStudentName}</strong> in the Hall of Fame!<br/><br/>");
        }

        if (recommendedAssignments is { Count: > 0 })
        {
            bodyBuilder.AppendLine("Here are some exercises you can work on to earn XP:<br/>");
            bodyBuilder.AppendLine("<ul>");
            foreach (var assignment in recommendedAssignments)
            {
                string assignmentUrl = !string.IsNullOrWhiteSpace(assignment.WebUrl)
                    ? assignment.WebUrl
                    : gamificationUri.AbsoluteUri;
                bodyBuilder.AppendLine($"<li><a href=\"{assignmentUrl}\">{assignment.TopicDescription} - {assignment.AssignmentDescription}</a> ({assignment.GreenTestsCount}/{assignment.TotalTestsCount} tests passed)</li>");
            }
            bodyBuilder.AppendLine("</ul><br/>");
        }

        bodyBuilder.AppendLine($"Visit your <a href=\"{gamificationUri.AbsoluteUri}\">Gamification Dashboard</a> to track your progress.<br/><br/>");
        bodyBuilder.AppendLine("Happy coding!<br/>");
        bodyBuilder.AppendLine("</p>");
        bodyBuilder.AppendLine("</body></html>");

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = subject,
            Body = bodyBuilder.ToString(),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(user.Email));

        await smtpClient.SendMailAsync(message);
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
            From = new MailAddress(fromEmail),
            Subject = subject,
            Body = bodyBuilder.ToString(),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(user.Email));

        await smtpClient.SendMailAsync(message);
    }
}
