using RequestsBL.Dtos;
using RequestsDAL.Entities;

namespace RequestsBL.Mapping;

/// <summary>The single place where Request entities are converted to DTOs.</summary>
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
}
