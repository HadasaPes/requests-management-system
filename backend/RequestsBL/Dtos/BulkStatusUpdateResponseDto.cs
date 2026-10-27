using System.Text.Json.Serialization;

namespace RequestsBL.Dtos;

/// <summary>Result of a bulk status change: one result per item, in the order the items were sent.</summary>
public class BulkStatusUpdateResponseDto
{
    /// <summary>Number of items in the request.</summary>
    public int Requested { get; init; }

    /// <summary>Number of items with outcome Updated.</summary>
    public int Succeeded { get; init; }

    /// <summary>Number of items with any other outcome.</summary>
    public int Failed { get; init; }

    public IReadOnlyList<BulkStatusUpdateItemResultDto> Results { get; init; } = [];
}

/// <summary>What happened to one item of a bulk status change.</summary>
public class BulkStatusUpdateItemResultDto
{
    public int Id { get; init; }

    /// <summary>Updated, NotFound, Conflict or InvalidTransition.</summary>
    public string Outcome { get; init; } = "";

    /// <summary>The new rowVersion (only when Updated).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RowVersion { get; init; }

    /// <summary>Why the item was not updated (only when not Updated).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }
}
