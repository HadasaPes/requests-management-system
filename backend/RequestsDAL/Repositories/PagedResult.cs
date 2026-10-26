namespace RequestsDAL.Repositories;

/// <summary>One page of rows plus the total number of rows that match the query.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
