using System.Net;
using System.Net.Http.Json;
using RequestsApi.Tests.Infrastructure;
using RequestsBL.Dtos;

namespace RequestsApi.Tests.Integration;

public class HistoryTests : IntegrationTestBase
{
    public HistoryTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHistory_AfterSuccessfulUpdate_ReturnsEntryWithAllFields()
    {
        // Arrange
        var request = await GetRequestAsync(TestData.Ids.InitechNew);
        var startedAt = DateTime.UtcNow;
        var patch = await PatchStatusAsync(request.Id, "InProgress", request.RowVersion, "History Tester");
        var finishedAt = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        // Act
        var history = await Client.GetFromJsonAsync<List<RequestStatusHistoryDto>>($"/api/requests/{request.Id}/history");

        // Assert: one entry, with every field filled from the update.
        var entry = Assert.Single(history!);
        Assert.Equal("New", entry.PreviousStatus);
        Assert.Equal("InProgress", entry.NewStatus);
        Assert.Equal("History Tester", entry.ChangedBy);
        Assert.Equal(DateTimeKind.Utc, entry.ChangedAt.Kind);
        Assert.InRange(entry.ChangedAt, startedAt.AddSeconds(-1), finishedAt.AddSeconds(1));
    }

    [Fact]
    public async Task GetHistory_UnknownId_Returns404()
    {
        // Act
        var response = await Client.GetAsync($"/api/requests/{TestData.Ids.Missing}/history");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
