namespace RequestsDAL.Repositories;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
