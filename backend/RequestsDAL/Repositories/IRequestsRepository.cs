using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

public interface IRequestsRepository
{
    /// <summary>Returns only the requested page plus the total count. Filtering, sorting and paging run in SQL.</summary>
    Task<PagedResult<Request>> GetPagedAsync(RequestQuery query, CancellationToken cancellationToken);

    /// <summary>Returns the request, or null if it does not exist.</summary>
    Task<Request?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Counts all requests per status and per priority with one GROUP BY query.</summary>
    Task<RequestCounts> GetCountsAsync(CancellationToken cancellationToken);

    /// <summary>Returns the request tracked for a later <see cref="UpdateStatusAsync"/>, or null if it does not exist.</summary>
    Task<Request?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken);

    /// <summary>Returns the existing requests among <paramref name="ids"/>, tracked, in one query.</summary>
    Task<IReadOnlyList<Request>> GetByIdsForUpdateAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the status of a tracked request and adds the audit row, in one transaction (its own SaveChanges).
    /// The database only updates the row if its RowVersion still equals
    /// <paramref name="expectedRowVersion"/>; otherwise nothing is saved, the request is detached
    /// and Conflict is returned. On success, <paramref name="request"/> holds the new RowVersion.
    /// </summary>
    Task<StatusUpdateResult> UpdateStatusAsync(
        Request request,
        RequestStatus newStatus,
        byte[] expectedRowVersion,
        string changedBy,
        DateTime changedAtUtc,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    /// <summary>Status history of one request, newest first.</summary>
    Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken);
}
