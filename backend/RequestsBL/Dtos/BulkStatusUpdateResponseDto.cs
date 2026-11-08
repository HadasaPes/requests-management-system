using System.Text.Json.Serialization;

namespace RequestsBL.Dtos;

public class BulkStatusUpdateResponseDto
{
    public int Requested { get; init; }

    public int Succeeded { get; init; }

    public int Failed { get; init; }

    public IReadOnlyList<BulkStatusUpdateItemResultDto> Results { get; init; } = [];
}

public class BulkStatusUpdateItemResultDto
{
    public int Id { get; init; }

    public string Outcome { get; init; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RowVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }
}
