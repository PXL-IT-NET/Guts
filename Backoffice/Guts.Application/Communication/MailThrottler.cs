using System;
using System.Threading;
using System.Threading.Tasks;

namespace Guts.Application.Communication;

public class MailThrottler : IMailThrottler
{
    public TimeSpan ThrottleDelay { get; set; }

    public MailThrottler(TimeSpan throttleDelay)
    {
        ThrottleDelay = throttleDelay;
    }

    public async Task ThrottleAsync(CancellationToken cancellationToken = default)
    {
        if (ThrottleDelay > TimeSpan.Zero)
        {
            await Task.Delay(ThrottleDelay, cancellationToken);
        }
    }
}

