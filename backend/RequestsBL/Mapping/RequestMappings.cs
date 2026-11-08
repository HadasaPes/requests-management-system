using RequestsBL.Dtos;
using RequestsBL.Rules;
using RequestsDAL.Entities;
using RequestsDAL.Repositories;

namespace RequestsBL.Mapping;

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
        CreatedAt = DateTime.SpecifyKind(request.CreatedAt, DateTimeKind.Utc),
        UpdatedAt = DateTime.SpecifyKind(request.UpdatedAt, DateTimeKind.Utc),
        RowVersion = Convert.ToBase64String(request.RowVersion),
        AllowedNextStatuses = StatusTransitions.GetAllowedNext(request.Status).Select(s => s.ToString()).ToList()
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

    private static Dictionary<string, int> CountForEveryValue<TEnum>(IReadOnlyDictionary<TEnum, int> counts)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().ToDictionary(
            value => value.ToString(),
            value => counts.GetValueOrDefault(value));
}
