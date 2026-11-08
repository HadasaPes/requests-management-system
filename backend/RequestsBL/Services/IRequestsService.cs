using RequestsBL.Dtos;
using RequestsBL.Exceptions;

namespace RequestsBL.Services;

public interface IRequestsService
{
    Task<PagedResponse<RequestDto>> GetRequestsAsync(RequestQueryParameters parameters, CancellationToken cancellationToken);

    Task<RequestDto> GetRequestByIdAsync(int id, CancellationToken cancellationToken);

    Task<RequestStatsDto> GetStatsAsync(CancellationToken cancellationToken);

    Task<RequestDto> UpdateStatusAsync(int id, UpdateStatusRequestDto update, CancellationToken cancellationToken);

    Task<BulkStatusUpdateResponseDto> BulkUpdateStatusAsync(BulkStatusUpdateRequestDto update, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestStatusHistoryDto>> GetHistoryAsync(int id, CancellationToken cancellationToken);
}
