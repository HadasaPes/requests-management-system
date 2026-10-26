using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

/// <summary>
/// Number of requests per status and per priority. Values that have no requests are absent.
/// </summary>
public record RequestCounts(
    IReadOnlyDictionary<RequestStatus, int> ByStatus,
    IReadOnlyDictionary<RequestPriority, int> ByPriority);
