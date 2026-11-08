using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

// Values that have no requests are absent.
public record RequestCounts(
    IReadOnlyDictionary<RequestStatus, int> ByStatus,
    IReadOnlyDictionary<RequestPriority, int> ByPriority);
