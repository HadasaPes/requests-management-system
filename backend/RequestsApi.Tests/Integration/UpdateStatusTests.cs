using System.Net;
using System.Net.Http.Json;
using RequestsApi.Tests.Infrastructure;
using RequestsBL.Dtos;
using RequestsDAL.Entities;

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

        // Act
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
    public async Task UpdateStatus_InvalidTransition_Returns400AndChangesNothing()
    {
        // Arrange
        var request = await GetRequestAsync(TestData.Ids.Completed);
        var before = (await GetStoredRequestsAsync()).Single(r => r.Id == request.Id);

        // Act
        var response = await PatchStatusAsync(request.Id, "New", request.RowVersion);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["status"], await ReadErrorFieldsAsync(response));

        var after = (await GetStoredRequestsAsync()).Single(r => r.Id == request.Id);
        Assert.Equal(before, after);
        Assert.Equal(RequestStatus.Completed, after.Status);
        Assert.Equal(0, await CountHistoryRowsAsync(request.Id));
    }

    [Fact]
    public async Task UpdateStatus_UnknownId_Returns404()
    {
        // Arrange
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
        // Arrange
        var before = await Client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats");
        var request = await GetRequestAsync(TestData.Ids.InitechNew);

        // Act
        var patch = await PatchStatusAsync(request.Id, "InProgress", request.RowVersion);
        var after = await Client.GetFromJsonAsync<RequestStatsDto>("/api/requests/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal(8, before!.CountByStatus["New"]);
        Assert.Equal(2, before.CountByStatus["InProgress"]);
        Assert.Equal(7, after!.CountByStatus["New"]);
        Assert.Equal(3, after.CountByStatus["InProgress"]);
        Assert.Equal(before.TotalCount, after.TotalCount);
    }
}
