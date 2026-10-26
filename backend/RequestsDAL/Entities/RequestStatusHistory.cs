namespace RequestsDAL.Entities;

public class RequestStatusHistory
{
    public int Id { get; set; }

    public int RequestId { get; set; }

    public Request Request { get; set; } = null!;

    public RequestStatus PreviousStatus { get; set; }

    public RequestStatus NewStatus { get; set; }

    /// <summary>UTC.</summary>
    public DateTime ChangedAt { get; set; }

    public string ChangedBy { get; set; } = string.Empty;
}
