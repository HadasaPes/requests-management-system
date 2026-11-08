namespace RequestsBL.Dtos;

public record RequestStatusHistoryDto
{
    public int Id { get; init; }

    public string PreviousStatus { get; init; } = string.Empty;

    public string NewStatus { get; init; } = string.Empty;

    public DateTime ChangedAt { get; init; }

    public string ChangedBy { get; init; } = string.Empty;
}
