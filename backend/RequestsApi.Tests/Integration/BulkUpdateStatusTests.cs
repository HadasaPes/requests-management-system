using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RequestsApi.Tests.Infrastructure;
using RequestsBL.Dtos;
using RequestsDAL.Entities;

namespace RequestsApi.Tests.Integration;

public class BulkUpdateStatusTests : IntegrationTestBase
{
    public BulkUpdateStatusTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task BulkUpdateStatus_MixedItems_ReturnsOutcomePerItemAndChangesOnlyValidItems()
    {
        // Arrange
        var valid1 = await GetRequestAsync(TestData.Ids.AcmeNewLow);
        var valid2 = await GetRequestAsync(TestData.Ids.GlobexNewHigh);
        var stale = await GetRequestAsync(TestData.Ids.AcmeNewHighFirst);
        var completed = await GetRequestAsync(TestData.Ids.Completed);

        // Someone else changes the stale request after we read it (a no-op UPDATE still changes the rowversion).
        await using (var dbContext = Fixture.CreateDbContext())
        {
            await dbContext.Database.ExecuteSqlAsync($"UPDATE Requests SET Title = Title WHERE Id = {stale.Id}");
        }

        var body = new
        {
            status = "InProgress",
            changedBy = "Test User",
            items = new[]
            {
                new { id = valid1.Id, rowVersion = valid1.RowVersion },
                new { id = TestData.Ids.Missing, rowVersion = "AAAAAAAAB9E=" },
                new { id = stale.Id, rowVersion = stale.RowVersion },
                new { id = completed.Id, rowVersion = completed.RowVersion },
                new { id = valid2.Id, rowVersion = valid2.RowVersion }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/requests/bulk-status", body);

        // Assert: the response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<BulkStatusUpdateResponseDto>())!;
        Assert.Equal((5, 2, 3), (result.Requested, result.Succeeded, result.Failed));
        Assert.Equal(
            [
                (valid1.Id, "Updated"),
                (TestData.Ids.Missing, "NotFound"),
                (stale.Id, "Conflict"),
                (completed.Id, "InvalidTransition"),
                (valid2.Id, "Updated")
            ],
            result.Results.Select(r => (r.Id, r.Outcome)));
        Assert.All(result.Results.Where(r => r.Outcome == "Updated"), r => Assert.NotNull(r.RowVersion));
        Assert.All(result.Results.Where(r => r.Outcome != "Updated"), r => Assert.False(string.IsNullOrEmpty(r.Error)));

        // Assert: the database - only the valid items changed, each with exactly one history row.
        await using var db = Fixture.CreateDbContext();
        var rows = await db.Requests
            .Where(r => new[] { valid1.Id, valid2.Id, stale.Id, completed.Id }.Contains(r.Id))
            .Select(r => new { r.Id, r.Status, HistoryRows = r.StatusHistory.Count })
            .ToDictionaryAsync(r => r.Id);
        Assert.Equal((RequestStatus.InProgress, 1), (rows[valid1.Id].Status, rows[valid1.Id].HistoryRows));
        Assert.Equal((RequestStatus.InProgress, 1), (rows[valid2.Id].Status, rows[valid2.Id].HistoryRows));
        Assert.Equal((RequestStatus.New, 0), (rows[stale.Id].Status, rows[stale.Id].HistoryRows));
        Assert.Equal((RequestStatus.Completed, 0), (rows[completed.Id].Status, rows[completed.Id].HistoryRows));
    }

    [Theory]
    [InlineData("101 items")]
    [InlineData("duplicate ids")]
    [InlineData("empty list")]
    public async Task BulkUpdateStatus_InvalidRequest_Returns400AndChangesNothing(string invalidCase)
    {
        // Arrange: the items use the real, current rowVersions, so without the request-level
        // validation the New requests among them would be updated.
        var before = await GetStoredRequestsAsync();
        var historyBefore = await CountAllHistoryRowsAsync();
        var realItems = before.Select(r => new { id = r.Id, rowVersion = r.RowVersion }).ToList();

        var items = invalidCase switch
        {
            // 12 real requests + 89 ids that do not exist = 101 items.
            "101 items" => realItems
                .Concat(Enumerable.Range(1_000, 101 - realItems.Count).Select(id => new { id, rowVersion = "AAAAAAAAB9E=" }))
                .ToList(),
            // A New request twice, plus another valid New request.
            "duplicate ids" => realItems
                .Where(i => i.id is TestData.Ids.AcmeNewLow or TestData.Ids.GlobexNewHigh)
                .Append(realItems.Single(i => i.id == TestData.Ids.AcmeNewLow))
                .ToList(),
            "empty list" => realItems.Take(0).ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(invalidCase))
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/requests/bulk-status",
            new { status = "InProgress", changedBy = "Test User", items });

        // Assert: the response
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["items"], await ReadErrorFieldsAsync(response));

        // Assert: the database - no request changed (status, RowVersion, UpdatedAt) and no history row was added.
        Assert.Equal(before, await GetStoredRequestsAsync());
        Assert.Equal(historyBefore, await CountAllHistoryRowsAsync());
    }
}
