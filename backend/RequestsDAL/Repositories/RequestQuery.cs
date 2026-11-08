using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

// Already validated; all dates are UTC.
public class RequestQuery
{
    public RequestStatus? Status { get; init; }

    public RequestPriority? Priority { get; init; }

    // Exact match.
    public string? OrganizationName { get; init; }

    // Exact match.
    public string? AssignedTo { get; init; }

    // Inclusive.
    public DateTime? CreatedFrom { get; init; }

    // Exclusive: CreatedAt < CreatedBefore.
    public DateTime? CreatedBefore { get; init; }

    // Contained in Title or OrganizationName.
    public string? Search { get; init; }

    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;

    public bool SortDescending { get; init; } = true;

    // 1-based.
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
