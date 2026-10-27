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
}
