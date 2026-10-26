using RequestsBL.Dtos;
using RequestsBL.Exceptions;
using RequestsDAL.Entities;
using RequestsDAL.Repositories;

namespace RequestsBL.Validation;

/// <summary>Validates list query parameters and converts them into a DAL query.</summary>
public static class RequestQueryValidator
{
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 100;

    // Guards against int overflow in (page - 1) * pageSize.
    public const int MaxPage = 1_000_000;

    /// <exception cref="ValidationException">One or more parameters are invalid; all errors are reported together.</exception>
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

        var status = ParseOptionalEnum<RequestStatus>(parameters.Status, "status", errors);
        var priority = ParseOptionalEnum<RequestPriority>(parameters.Priority, "priority", errors);
        var sortBy = ParseOptionalEnum<RequestSortField>(parameters.SortBy, "sortBy", errors) ?? RequestSortField.CreatedAt;

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

    /// <summary>
    /// Matches enum names only (case-insensitive). Unlike Enum.TryParse, numbers such as "1" are rejected.
    /// </summary>
    private static TEnum? ParseOptionalEnum<TEnum>(string? value, string field, Dictionary<string, string[]> errors)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var names = Enum.GetNames<TEnum>();
        var match = names.FirstOrDefault(name => name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return Enum.Parse<TEnum>(match);
        }

        var allowed = string.Join(", ", names.Select(name => FormatAllowedName(name, field)));
        errors[field] = [$"'{trimmed}' is not a valid {field}. Allowed values: {allowed}."];
        return null;
    }

    // sortBy values are shown in camelCase (createdAt) to match the query-string style; enum values as-is (InProgress).
    private static string FormatAllowedName(string name, string field) =>
        field == "sortBy" ? char.ToLowerInvariant(name[0]) + name[1..] : name;

    // Model binding turns "...Z" / "+02:00" into local time; values without an offset are treated as UTC.
    private static DateTime? ToUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
        { Kind: DateTimeKind.Unspecified } unspecified => DateTime.SpecifyKind(unspecified, DateTimeKind.Utc),
        { } utc => utc
    };

    // createdTo is inclusive for clients. A date without a time part (e.g. 2025-01-31) means the whole day,
    // so the bound is the start of the next day; otherwise it is the exact time plus one tick
    // (datetime2(7) resolution), so "< bound" equals "<= createdTo".
    private static DateTime? ToExclusiveUpperBound(DateTime? createdTo) => createdTo switch
    {
        null => null,
        { TimeOfDay.Ticks: 0 } date => date.AddDays(1),
        { } exact => exact.AddTicks(1)
    };

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
