using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

public class RequestQuery
{
    public RequestStatus? Status { get; init; }

    public RequestPriority? Priority { get; init; }

    public string? OrganizationName { get; init; }

    public string? AssignedTo { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedBefore { get; init; }

    public string? Search { get; init; }

    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;

    public bool SortDescending { get; init; } = true;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
