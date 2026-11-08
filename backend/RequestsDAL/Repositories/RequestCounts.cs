using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

public record RequestCounts(
    IReadOnlyDictionary<RequestStatus, int> ByStatus,
    IReadOnlyDictionary<RequestPriority, int> ByPriority);
