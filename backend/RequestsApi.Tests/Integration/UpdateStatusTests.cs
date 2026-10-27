using System.Net;
using System.Net.Http.Json;
using RequestsApi.Tests.Infrastructure;
using RequestsBL.Dtos;

namespace RequestsApi.Tests.Integration;

public class UpdateStatusTests : IntegrationTestBase
{
    public UpdateStatusTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_OneSucceedsAndOneConflicts()
    {
        // Arrange
        var request = await GetRequestAsync(TestData.Ids.InitechNew);

        // Act: both PATCHes are started before either is awaited, so they run at the same time.
        var responses = await Task.WhenAll(
            PatchStatusAsync(request.Id, "InProgress", request.RowVersion, "User A"),
            PatchStatusAsync(request.Id, "InProgress", request.RowVersion, "User B"));

        // Assert
        var statusCodes = responses.Select(r => r.StatusCode).Order().ToList();
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], statusCodes);
        Assert.Equal(1, await CountHistoryRowsAsync(request.Id));
        Assert.Equal("InProgress", (await GetRequestAsync(request.Id)).Status);
    }

    [Fact]
    public async Task UpdateStatus_UnknownId_Returns404()
    {
        // Arrange: a well-formed body, so only the id can cause the error.
        const string anyRowVersion = "AAAAAAAAB9E=";

        // Act
        var response = await PatchStatusAsync(TestData.Ids.Missing, "InProgress", anyRowVersion);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetStats_AfterSuccessfulStatusUpdate_ReturnsUpdatedCountsImmediately()
    {
        // Arrange: the first call stores the stats in the cache.
        var before = await Client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats");
        var request = await GetRequestAsync(TestData.Ids.InitechNew);

        // Act
        var patch = await PatchStatusAsync(request.Id, "InProgress", request.RowVersion);
        var after = await Client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats");

        // Assert: the seed has 8 New and 2 InProgress.
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal(8, before!.CountByStatus["New"]);
        Assert.Equal(2, before.CountByStatus["InProgress"]);
        Assert.Equal(7, after!.CountByStatus["New"]);
        Assert.Equal(3, after.CountByStatus["InProgress"]);
        Assert.Equal(before.TotalCount, after.TotalCount);
    }
}
