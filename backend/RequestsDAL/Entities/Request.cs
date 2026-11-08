namespace RequestsDAL.Entities;

public class Request
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }

    public RequestPriority Priority { get; set; }

    public string? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
}
