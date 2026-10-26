namespace RequestsBL.Dtos;

/// <summary>Body of a status change.</summary>
public class UpdateStatusRequestDto
{
    /// <summary>New status: New, InProgress, Waiting or Completed (case-insensitive).</summary>
    public string? Status { get; set; }

    /// <summary>The rowVersion returned by the last GET of this request (base64).</summary>
    public string? RowVersion { get; set; }

    /// <summary>Who makes the change (max 100 characters).</summary>
    public string? ChangedBy { get; set; }
}
