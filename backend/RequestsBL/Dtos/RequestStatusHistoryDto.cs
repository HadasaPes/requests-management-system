namespace RequestsBL.Dtos;

/// <summary>One status change of a request.</summary>
public record RequestStatusHistoryDto
{
    public int Id { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    /// <summary>UTC.</summary>
    public DateTime ChangedAt { get; init; }

    public string ChangedBy { get; init; } = string.Empty;
}
