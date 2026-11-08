namespace RequestsBL.Dtos;

public class BulkStatusUpdateRequestDto
{
    public string? Status { get; set; }

    public string? ChangedBy { get; set; }

    public List<BulkStatusUpdateItemDto?>? Items { get; set; }
}

public class BulkStatusUpdateItemDto
{
    public int? Id { get; set; }

    public string? RowVersion { get; set; }
}
