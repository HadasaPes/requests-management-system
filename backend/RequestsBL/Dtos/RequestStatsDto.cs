namespace RequestsBL.Dtos;

/// <summary>Dashboard summary of all requests (no filters).</summary>
public record RequestStatsDto
{
    public int TotalCount { get; init; }

    /// <summary>Count for every status (New, InProgress, Waiting, Completed), including zeros.</summary>
    public IReadOnlyDictionary<string, int> CountByStatus { get; init; } = new Dictionary<string, int>();

    /// <summary>Count for every priority (Low, Medium, High), including zeros.</summary>
    public IReadOnlyDictionary<string, int> CountByPriority { get; init; } = new Dictionary<string, int>();
}
