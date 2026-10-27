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

    /// <summary>Changes the status, checking the workflow and the client's rowVersion, and records the change.</summary>
    /// <exception cref="ValidationException">Invalid input or a transition that is not allowed.</exception>
    /// <exception cref="NotFoundException">No request with this id exists.</exception>
    /// <exception cref="ConflictException">The request was changed by someone else since the client read it.</exception>
    Task<RequestDto> UpdateStatusAsync(int id, UpdateStatusRequestDto update, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the status of up to 100 requests with partial success: each item is checked and saved
    /// in its own transaction, and gets its own result (Updated, NotFound, Conflict, InvalidTransition).
    /// </summary>
    /// <exception cref="ValidationException">The request itself is invalid; no item was processed.</exception>
    Task<BulkStatusUpdateResponseDto> BulkUpdateStatusAsync(BulkStatusUpdateRequestDto update, CancellationToken cancellationToken);

    /// <summary>Status changes of one request, newest first.</summary>
    /// <exception cref="NotFoundException">No request with this id exists.</exception>
    Task<IReadOnlyList<RequestStatusHistoryDto>> GetHistoryAsync(int id, CancellationToken cancellationToken);
}
