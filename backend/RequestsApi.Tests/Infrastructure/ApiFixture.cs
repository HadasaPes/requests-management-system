using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using RequestsDAL.Data;

namespace RequestsApi.Tests.Infrastructure;

public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DatabaseName = "RequestsDb_Tests";

    private const string ConnectionString =
        $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:RequestsDb", ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();

        var actualDatabase = dbContext.Database.GetDbConnection().Database;
        if (actualDatabase != DatabaseName)
        {
            throw new InvalidOperationException($"Tests must use {DatabaseName}, but the API is configured for '{actualDatabase}'.");
        }

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RequestsDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM RequestStatusHistory; DELETE FROM Requests;");

        await dbContext.Database.OpenConnectionAsync();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Requests ON");
        dbContext.Requests.AddRange(TestData.CreateRequests());
        await dbContext.SaveChangesAsync();
        await dbContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Requests OFF");
        await transaction.CommitAsync();

        ((MemoryCache)Services.GetRequiredService<IMemoryCache>()).Clear();
    }

    public RequestsDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<RequestsDbContext>().UseSqlServer(ConnectionString).Options);

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
