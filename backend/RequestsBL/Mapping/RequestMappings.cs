using RequestsBL.Dtos;
using RequestsDAL.Entities;
using RequestsDAL.Repositories;

namespace RequestsBL.Mapping;

/// <summary>The single place where Request data from the DAL is converted to DTOs.</summary>
public static class RequestMappings
{
    public static RequestDto ToDto(this Request request) => new()
    {
        Id = request.Id,
        Title = request.Title,
        OrganizationName = request.OrganizationName,
        Status = request.Status.ToString(),
        Priority = request.Priority.ToString(),
        AssignedTo = request.AssignedTo,
        // datetime2 has no time zone; values are stored as UTC, so mark them as UTC ("Z" in JSON).
        CreatedAt = DateTime.SpecifyKind(request.CreatedAt, DateTimeKind.Utc),
        UpdatedAt = DateTime.SpecifyKind(request.UpdatedAt, DateTimeKind.Utc),
        RowVersion = Convert.ToBase64String(request.RowVersion)
    };

    public static RequestStatusHistoryDto ToDto(this RequestStatusHistory history) => new()
    {
        Id = history.Id,
        PreviousStatus = history.PreviousStatus.ToString(),
        NewStatus = history.NewStatus.ToString(),
        ChangedAt = DateTime.SpecifyKind(history.ChangedAt, DateTimeKind.Utc),
        ChangedBy = history.ChangedBy
    };

    public static RequestStatsDto ToStatsDto(this RequestCounts counts)
    {
        var byStatus = CountForEveryValue(counts.ByStatus);

        return new RequestStatsDto
        {
            TotalCount = byStatus.Values.Sum(),
            CountByStatus = byStatus,
            CountByPriority = CountForEveryValue(counts.ByPriority)
        };
    }

    // Lists every enum value in declaration order, with 0 for values that have no requests.
    private static Dictionary<string, int> CountForEveryValue<TEnum>(IReadOnlyDictionary<TEnum, int> counts)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().ToDictionary(
            value => value.ToString(),
            value => counts.GetValueOrDefault(value));
}
