using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RequestsBL.Dtos;
using RequestsDAL.Entities;

namespace RequestsApi.Tests.Infrastructure;

/// <summary>Restores the known data before each test and provides the shared HTTP and database helpers.</summary>
[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(ApiFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
    }

    protected ApiFixture Fixture { get; }

    protected HttpClient Client { get; }

    public Task InitializeAsync() => Fixture.ResetAsync();

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<RequestDto> GetRequestAsync(int id) =>
        (await Client.GetFromJsonAsync<RequestDto>($"/api/requests/{id}"))!;

    protected Task<HttpResponseMessage> PatchStatusAsync(int id, string status, string rowVersion, string changedBy = "Test User") =>
        Client.PatchAsJsonAsync($"/api/requests/{id}/status", new { status, rowVersion, changedBy });

    protected async Task<int> CountHistoryRowsAsync(int requestId)
    {
        await using var dbContext = Fixture.CreateDbContext();
        return await dbContext.RequestStatusHistory.CountAsync(h => h.RequestId == requestId);
    }

    /// <summary>Every request as stored in the database, to assert that a rejected call changed nothing.</summary>
    protected async Task<IReadOnlyList<StoredRequest>> GetStoredRequestsAsync()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var rows = await dbContext.Requests.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
        return rows.Select(r => new StoredRequest(r.Id, r.Status, Convert.ToBase64String(r.RowVersion), r.UpdatedAt)).ToList();
    }

    protected async Task<int> CountAllHistoryRowsAsync()
    {
        await using var dbContext = Fixture.CreateDbContext();
        return await dbContext.RequestStatusHistory.CountAsync();
    }

    /// <summary>The field names in the "errors" object of a 400 ValidationProblemDetails response.</summary>
    protected static async Task<IReadOnlyList<string>> ReadErrorFieldsAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("errors").EnumerateObject().Select(e => e.Name).ToList();
    }

    protected sealed record StoredRequest(int Id, RequestStatus Status, string RowVersion, DateTime UpdatedAt);
}
