using RequestsBL.Dtos;
using RequestsBL.Exceptions;

namespace RequestsBL.Services;

public interface IRequestsService
{
    /// <exception cref="ValidationException">The query parameters are invalid.</exception>
    Task<PagedResponse<RequestDto>> GetRequestsAsync(RequestQueryParameters parameters, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException">No request with this id exists.</exception>
    Task<RequestDto> GetRequestByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Counts of all requests per status and priority (global, no filters).</summary>
    Task<RequestStatsDto> GetStatsAsync(CancellationToken cancellationToken);
}
