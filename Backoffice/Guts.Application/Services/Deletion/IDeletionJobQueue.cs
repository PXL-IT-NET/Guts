using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Guts.Application.Services.Deletion;

public interface IDeletionJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
