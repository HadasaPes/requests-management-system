using RequestsBL.Exceptions;
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

    /// <exception cref="ValidationException">The transition is not allowed.</exception>
    public static void EnsureAllowed(RequestStatus from, RequestStatus to)
    {
        if (from == to)
        {
            throw new ValidationException("status", $"The request is already {from}.");
        }

        if (!IsAllowed(from, to))
        {
            throw new ValidationException("status", $"Cannot change status from {from} to {to}.");
        }
    }
}
