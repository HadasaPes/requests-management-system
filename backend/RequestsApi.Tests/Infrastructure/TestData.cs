using RequestsDAL.Entities;

namespace RequestsApi.Tests.Infrastructure;

/// <summary>
/// The known data set, restored before every integration test.
///   Ids 1-5:  Acme Ltd, New, High, created on 1-5 Jan 2026 (the combined-filter target)
///   Ids 6-12: one difference each, so every filter is needed to exclude them
/// Totals - Status: New 8, InProgress 2, Waiting 1, Completed 1. Priority: Low 2, Medium 3, High 7.
/// </summary>
public static class TestData
{
    public const string Acme = "Acme Ltd";

    public static class Ids
    {
        public const int AcmeNewHighFirst = 1;
        public const int AcmeNewLow = 6;
        public const int GlobexNewHigh = 8;
        public const int Completed = 10;
        public const int InitechNew = 12;
        public const int Missing = 999_999;
    }

    public static IReadOnlyList<Request> CreateRequests() =>
    [
        Create(1, Acme, RequestStatus.New, RequestPriority.High, day: 1),
        Create(2, Acme, RequestStatus.New, RequestPriority.High, day: 2),
        Create(3, Acme, RequestStatus.New, RequestPriority.High, day: 3),
        Create(4, Acme, RequestStatus.New, RequestPriority.High, day: 4),
        Create(5, Acme, RequestStatus.New, RequestPriority.High, day: 5),
        Create(6, Acme, RequestStatus.New, RequestPriority.Low, day: 3),
        Create(7, Acme, RequestStatus.InProgress, RequestPriority.High, day: 3),
        Create(8, "Globex", RequestStatus.New, RequestPriority.High, day: 3),
        Create(9, "Globex", RequestStatus.Waiting, RequestPriority.Medium, day: 6),
        Create(10, "Initech", RequestStatus.Completed, RequestPriority.Low, day: 7),
        Create(11, "Initech", RequestStatus.InProgress, RequestPriority.Medium, day: 8),
        Create(12, "Initech", RequestStatus.New, RequestPriority.Medium, day: 9)
    ];

    private static Request Create(int id, string organization, RequestStatus status, RequestPriority priority, int day)
    {
        var createdAt = new DateTime(2026, 1, day, 10, 0, 0, DateTimeKind.Utc);
        return new Request
        {
            Id = id,
            Title = $"Test request {id}",
            OrganizationName = organization,
            Status = status,
            Priority = priority,
            AssignedTo = status == RequestStatus.New ? null : "Test Handler",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
