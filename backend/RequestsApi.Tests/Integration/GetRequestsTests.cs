using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RequestsApi.Tests.Infrastructure;
using RequestsBL.Dtos;

namespace RequestsApi.Tests.Integration;

public class GetRequestsTests : IntegrationTestBase
{
    public GetRequestsTests(ApiFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetRequests_CombinedFiltersAndPaging_ReturnsExactPageAndTotalCount()
    {
        // Arrange: ids 2-5 match all filters (id 1 is created before createdFrom; 6, 7, 8 differ in one field each).
        var url = "/api/requests?status=New&priority=High&organizationName=Acme%20Ltd&createdFrom=2026-01-02"
                  + "&sortBy=createdAt&sortDirection=asc&page=2&pageSize=2";

        // Act
        var page = await Client.GetFromJsonAsync<PagedResponse<RequestDto>>(url);

        // Assert
        Assert.NotNull(page);
        Assert.Equal(4, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(2, page.Page);
        Assert.Equal([4, 5], page.Items.Select(r => r.Id));
        Assert.All(page.Items, r =>
        {
            Assert.Equal("New", r.Status);
            Assert.Equal("High", r.Priority);
            Assert.Equal(TestData.Acme, r.OrganizationName);
        });
    }

    [Theory]
    [InlineData("pageSize=500", "pageSize")]
    [InlineData("status=Done", "status")]
    public async Task GetRequests_InvalidQueryParameter_Returns400WithFieldError(string query, string expectedField)
    {
        // Act
        var response = await Client.GetAsync($"/api/requests?{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.Equal([expectedField], errors.EnumerateObject().Select(e => e.Name));
    }
}
