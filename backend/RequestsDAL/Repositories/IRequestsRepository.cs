using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

public interface IRequestsRepository
{
    /// <summary>Returns only the requested page plus the total count. Filtering, sorting and paging run in SQL.</summary>
    Task<PagedResult<Request>> GetPagedAsync(RequestQuery query, CancellationToken cancellationToken);

    /// <summary>Returns the request, or null if it does not exist.</summary>
    Task<Request?> GetByIdAsync(int id, CancellationToken cancellationToken);
}
