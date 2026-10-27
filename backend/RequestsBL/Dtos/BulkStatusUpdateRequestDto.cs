namespace RequestsBL.Dtos;

/// <summary>Body of a bulk status change: one new status for up to 100 requests.</summary>
public class BulkStatusUpdateRequestDto
{
    /// <summary>New status for all items: New, InProgress, Waiting or Completed (case-insensitive).</summary>
    public string? Status { get; set; }

    /// <summary>Who makes the change (max 100 characters).</summary>
    public string? ChangedBy { get; set; }

    /// <summary>1 to 100 requests, each with the rowVersion the client last read. Ids must be unique.</summary>
    public List<BulkStatusUpdateItemDto?>? Items { get; set; }
}

/// <summary>One request in a bulk status change.</summary>
public class BulkStatusUpdateItemDto
{
    /// <summary>Request id.</summary>
    public int? Id { get; set; }

    /// <summary>The rowVersion returned by the last GET of this request (base64).</summary>
    public string? RowVersion { get; set; }
}
