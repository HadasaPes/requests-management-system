using RequestsBL.Dtos;
using RequestsBL.Exceptions;
using RequestsDAL.Entities;
using RequestsDAL.Repositories;

namespace RequestsBL.Validation;

public static class RequestQueryValidator
{
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 100;

    public const int MaxPage = 1_000_000;

    public static RequestQuery ToQuery(RequestQueryParameters parameters)
    {
        var errors = new Dictionary<string, string[]>();

        if (parameters.Page < 1 || parameters.Page > MaxPage)
        {
            errors["page"] = [$"Page must be between 1 and {MaxPage:N0}."];
        }

        if (parameters.PageSize < 1 || parameters.PageSize > MaxPageSize)
        {
            errors["pageSize"] = [$"Page size must be between 1 and {MaxPageSize}."];
        }

        var status = EnumParsing.ParseOptional<RequestStatus>(parameters.Status, "status", errors);
        var priority = EnumParsing.ParseOptional<RequestPriority>(parameters.Priority, "priority", errors);
        var sortBy = EnumParsing.ParseOptional<RequestSortField>(parameters.SortBy, "sortBy", errors, camelCaseNames: true)
            ?? RequestSortField.CreatedAt;

        var sortDescending = true;
        if (!string.IsNullOrWhiteSpace(parameters.SortDirection))
        {
            var direction = parameters.SortDirection.Trim();
            if (direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
            {
                sortDescending = false;
            }
            else if (!direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                errors["sortDirection"] = [$"'{direction}' is not a valid sort direction. Allowed values: asc, desc."];
            }
        }

        var createdFrom = ToUtc(parameters.CreatedFrom);
        var createdTo = ToUtc(parameters.CreatedTo);
        if (createdFrom > createdTo)
        {
            errors["createdFrom"] = ["createdFrom must not be later than createdTo."];
        }

        var search = NullIfBlank(parameters.Search);
        if (search?.Length > MaxSearchLength)
        {
            errors["search"] = [$"Search text must be at most {MaxSearchLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new RequestQuery
        {
            Status = status,
            Priority = priority,
            OrganizationName = NullIfBlank(parameters.OrganizationName),
            AssignedTo = NullIfBlank(parameters.AssignedTo),
            CreatedFrom = createdFrom,
            CreatedBefore = ToExclusiveUpperBound(createdTo),
            Search = search,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        { Kind: DateTimeKind.Unspecified } unspecified => DateTime.SpecifyKind(unspecified, DateTimeKind.Utc),
        { } utc => utc
    };

    private static DateTime? ToExclusiveUpperBound(DateTime? createdTo) => createdTo switch
    {
        null => null,
        { TimeOfDay.Ticks: 0 } date => date.AddDays(1),
        { } exact => exact.AddTicks(1)
    };

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
