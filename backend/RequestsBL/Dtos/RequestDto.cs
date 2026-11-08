namespace RequestsBL.Dtos;

public record RequestDto
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string OrganizationName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;

    public string? AssignedTo { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public string RowVersion { get; init; } = string.Empty;

    public IReadOnlyList<string> AllowedNextStatuses { get; init; } = [];
}
