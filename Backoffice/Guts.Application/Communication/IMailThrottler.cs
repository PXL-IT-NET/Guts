using System;
using System.Threading;
using System.Threading.Tasks;

namespace Guts.Application.Communication;

public interface IMailThrottler
{
    TimeSpan ThrottleDelay { get; set; }
    Task ThrottleAsync(CancellationToken cancellationToken = default);
}

