namespace RequestsDAL.Repositories;

public enum StatusUpdateResult
{
    Updated,

    /// <summary>The row was changed by someone else after the client read it (RowVersion no longer matches).</summary>
    Conflict
}
