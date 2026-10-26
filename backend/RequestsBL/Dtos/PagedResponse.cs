namespace RequestsBL.Dtos;

/// <summary>One page of results plus paging information.</summary>
public record PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>1-based page number.</summary>
    public int Page { get; init; }

    public int PageSize { get; init; }

    /// <summary>Number of items matching the filters, across all pages.</summary>
    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}
