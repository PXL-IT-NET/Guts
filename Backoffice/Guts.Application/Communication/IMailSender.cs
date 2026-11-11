using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos.Gamification;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Application.Communication;

public interface IMailSender
{
    Task SendConfirmUserEmailMessageAsync(User user, string confirmationToken);
    Task SendForgotPasswordMessageAsync(User user, string forgotPasswordToken);
    Task SendStreakReminderEmailAsync(
        User user,
        string courseName,
        int courseId,
        int currentStreak,
        string targetStudentName,
        IReadOnlyList<StreakReminderAssignmentDto> recommendedAssignments);
    Task SendPeerAssessmentReminderEmailAsync(
        User recipient,
        string courseName,
        int courseId,
        string projectCode,
        string projectDescription,
        IProjectAssessment assessment,
        int teamId);
}
