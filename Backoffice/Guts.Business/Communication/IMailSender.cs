using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Business.Dtos.Gamification;
using Guts.Domain.UserAggregate;

namespace Guts.Business.Communication;

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
}
