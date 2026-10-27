using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RequestsDAL.Data;

namespace RequestsApi.Tests.Infrastructure;

/// <summary>
/// One API host and one dedicated LocalDB database for all integration tests.
/// The database is dropped and recreated from the EF migrations once per test run;
/// <see cref="ResetAsync"/> restores the known data before every test.
/// </summary>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DatabaseName = "RequestsDb_Tests";

    private const string ConnectionString =
        $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Read by Program.cs before the app is built, so the API never sees the development database.
        builder.UseSetting("ConnectionStrings:RequestsDb", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();

        // Safety net: EnsureDeleted must never run against any other database.
        var actualDatabase = dbContext.Database.GetDbConnection().Database;
        if (actualDatabase != DatabaseName)
        {
            throw new InvalidOperationException($"Tests must use {DatabaseName}, but the API is configured for '{actualDatabase}'.");
        }

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>Restores the known requests (fixed ids), removes all history and empties the cache.</summary>
    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM RequestStatusHistory; DELETE FROM Requests;");

        // Explicit ids so tests can refer to them; IDENTITY_INSERT needs the same open connection.
        await dbContext.Database.OpenConnectionAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Requests ON");
        dbContext.Requests.AddRange(TestData.CreateRequests());
        await dbContext.SaveChangesAsync();
        await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Requests OFF");
        await transaction.CommitAsync();

        ((MemoryCache)Services.GetRequiredService<IMemoryCache>()).Clear();
    }

    /// <summary>A fresh DbContext on the test database, for arranging and asserting directly in SQL Server.</summary>
    public RequestsDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<RequestsDbContext>().UseSqlServer(ConnectionString).Options);

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

/// <summary>All integration test classes share one fixture and run one after another (one database).</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
