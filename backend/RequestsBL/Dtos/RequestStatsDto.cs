namespace RequestsBL.Dtos;

public record RequestStatsDto
{
    public int TotalCount { get; init; }

    public IReadOnlyDictionary<string, int> CountByStatus { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> CountByPriority { get; init; } = new Dictionary<string, int>();
}
