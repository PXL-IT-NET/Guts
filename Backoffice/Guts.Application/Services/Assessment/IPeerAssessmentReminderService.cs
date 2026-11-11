using System;
using System.Threading;
using System.Threading.Tasks;

namespace Guts.Application.Services.Assessment;

public interface IPeerAssessmentReminderService
{
    Task<int> SendAssessmentRemindersAsync(DateTime executionTime, CancellationToken cancellationToken = default);
}

