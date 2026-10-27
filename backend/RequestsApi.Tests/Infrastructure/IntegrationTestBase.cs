using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RequestsBL.Dtos;

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
}
