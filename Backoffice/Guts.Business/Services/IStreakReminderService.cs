using System;
using System.Threading;
using System.Threading.Tasks;

namespace Guts.Business.Services;

public interface IStreakReminderService
{
    Task<int> SendStreakRemindersAsync(DateTime executionTime, CancellationToken cancellationToken = default);
}

