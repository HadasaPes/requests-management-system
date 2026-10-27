using RequestsDAL.Entities;

namespace RequestsBL.Rules;

/// <summary>
/// The status workflow:
///   New        -> InProgress
///   InProgress -> Waiting, Completed
///   Waiting    -> InProgress
///   Completed  -> (final)
/// </summary>
public static class StatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> Allowed =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            [RequestStatus.New] = [RequestStatus.InProgress],
            [RequestStatus.InProgress] = [RequestStatus.Waiting, RequestStatus.Completed],
            [RequestStatus.Waiting] = [RequestStatus.InProgress],
            [RequestStatus.Completed] = []
        };

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => Allowed[from].Contains(to);

    /// <summary>The statuses a request can move to next; empty for Completed.</summary>
    public static IReadOnlyList<RequestStatus> GetAllowedNext(RequestStatus from) => Allowed[from];

    /// <summary>Returns why the transition is not allowed, or null if it is allowed.</summary>
    public static string? GetError(RequestStatus from, RequestStatus to)
    {
        if (from == to)
        {
            return $"The request is already {from}.";
        }

        return IsAllowed(from, to) ? null : $"Cannot change status from {from} to {to}.";
    }
}
