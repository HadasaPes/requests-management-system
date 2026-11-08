using System.ComponentModel;

namespace RequestsBL.Dtos;

public class RequestQueryParameters
{
    [DefaultValue(1)]
    public int Page { get; set; } = 1;

    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public string? OrganizationName { get; set; }

    public string? AssignedTo { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }

    public string? Search { get; set; }

    [DefaultValue("createdAt")]
    public string? SortBy { get; set; }

    [DefaultValue("desc")]
    public string? SortDirection { get; set; }
}
