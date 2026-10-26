using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

/// <summary>
/// Already-validated filters, search, sorting and paging for a requests list query.
/// All dates are UTC.
/// </summary>
public class RequestQuery
{
    public RequestStatus? Status { get; init; }

    public RequestPriority? Priority { get; init; }

    /// <summary>Exact match.</summary>
    public string? OrganizationName { get; init; }

    /// <summary>Exact match.</summary>
    public string? AssignedTo { get; init; }

    /// <summary>Inclusive.</summary>
    public DateTime? CreatedFrom { get; init; }

    /// <summary>Exclusive upper bound: CreatedAt &lt; CreatedBefore.</summary>
    public DateTime? CreatedBefore { get; init; }

    /// <summary>Text contained in Title or OrganizationName.</summary>
    public string? Search { get; init; }

    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;

    public bool SortDescending { get; init; } = true;

    /// <summary>1-based.</summary>
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
