using System.Collections.Generic;

namespace Guts.Application.Dtos;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int? PageSize { get; init; }
}

