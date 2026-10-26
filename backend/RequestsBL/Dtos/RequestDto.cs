namespace RequestsBL.Dtos;

/// <summary>A service request as returned to clients.</summary>
public record RequestDto
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string OrganizationName { get; init; } = string.Empty;

    /// <summary>New, InProgress, Waiting or Completed.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Low, Medium or High.</summary>
    public string Priority { get; init; } = string.Empty;

    public string? AssignedTo { get; init; }

    /// <summary>UTC.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>UTC.</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>Concurrency token (base64). Send it back unchanged when updating this request.</summary>
    public string RowVersion { get; init; } = string.Empty;
}
