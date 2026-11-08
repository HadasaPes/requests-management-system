using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

public interface IRequestsRepository
{
    Task<PagedResult<Request>> GetPagedAsync(RequestQuery query, CancellationToken cancellationToken);

    Task<Request?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<RequestCounts> GetCountsAsync(CancellationToken cancellationToken);

    Task<Request?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Request>> GetByIdsForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    Task<StatusUpdateResult> UpdateStatusAsync(
        Request request,
        RequestStatus newStatus,
        byte[] expectedRowVersion,
        string changedBy,
        DateTime changedAtUtc,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken);
}
