using System.ComponentModel;

namespace RequestsBL.Dtos;

/// <summary>Filters, search, sorting and paging for the requests list. Validated by the BL.</summary>
public class RequestQueryParameters
{
    /// <summary>Page number, starting at 1.</summary>
    [DefaultValue(1)]
    public int Page { get; set; } = 1;

    /// <summary>Items per page, 1 to 100.</summary>
    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;

    /// <summary>New, InProgress, Waiting or Completed (case-insensitive).</summary>
    public string? Status { get; set; }

    /// <summary>Low, Medium or High (case-insensitive).</summary>
    public string? Priority { get; set; }

    /// <summary>Exact organization name.</summary>
    public string? OrganizationName { get; set; }

    /// <summary>Exact handler name.</summary>
    public string? AssignedTo { get; set; }

    /// <summary>Created at or after this time (UTC if no offset is given).</summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Created at or before this time (UTC if no offset is given).
    /// A date without a time (e.g. 2025-01-31) includes that whole day.
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>Text contained in the title or organization name (max 100 characters).</summary>
    public string? Search { get; set; }

    /// <summary>createdAt, updatedAt, priority, status, title or organizationName.</summary>
    [DefaultValue("createdAt")]
    public string? SortBy { get; set; }

    /// <summary>asc or desc.</summary>
    [DefaultValue("desc")]
    public string? SortDirection { get; set; }
}
