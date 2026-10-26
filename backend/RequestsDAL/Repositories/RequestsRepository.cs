using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RequestsDAL.Data;
using RequestsDAL.Entities;

namespace RequestsDAL.Repositories;

public class RequestsRepository : IRequestsRepository
{
    private readonly RequestsDbContext _dbContext;

    public RequestsRepository(RequestsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<Request>> GetPagedAsync(RequestQuery query, CancellationToken cancellationToken)
    {
        // Nothing runs until CountAsync / ToListAsync: the IQueryable is translated to SQL.
        var filtered = ApplyFilters(_dbContext.Requests.AsNoTracking(), query);

        var totalCount = await filtered.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return new PagedResult<Request>([], 0);
        }

        var items = await ApplySorting(filtered, query.SortBy, query.SortDescending)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Request>(items, totalCount);
    }

    public Task<Request?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.Requests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    private static IQueryable<Request> ApplyFilters(IQueryable<Request> source, RequestQuery query)
    {
        if (query.Status is { } status)
        {
            source = source.Where(r => r.Status == status);
        }

        if (query.Priority is { } priority)
        {
            source = source.Where(r => r.Priority == priority);
        }

        if (query.OrganizationName is { } organizationName)
        {
            source = source.Where(r => r.OrganizationName == organizationName);
        }

        if (query.AssignedTo is { } assignedTo)
        {
            source = source.Where(r => r.AssignedTo == assignedTo);
        }

        if (query.CreatedFrom is { } createdFrom)
        {
            source = source.Where(r => r.CreatedAt >= createdFrom);
        }

        if (query.CreatedBefore is { } createdBefore)
        {
            source = source.Where(r => r.CreatedAt < createdBefore);
        }

        if (query.Search is { } search)
        {
            source = source.Where(r => r.Title.Contains(search) || r.OrganizationName.Contains(search));
        }

        return source;
    }

    private static IQueryable<Request> ApplySorting(IQueryable<Request> source, RequestSortField sortBy, bool descending) =>
        sortBy switch
        {
            RequestSortField.UpdatedAt => OrderBy(source, r => r.UpdatedAt, descending),
            RequestSortField.Priority => OrderBy(source, r => r.Priority, descending),
            RequestSortField.Status => OrderBy(source, r => r.Status, descending),
            RequestSortField.Title => OrderBy(source, r => r.Title, descending),
            RequestSortField.OrganizationName => OrderBy(source, r => r.OrganizationName, descending),
            _ => OrderBy(source, r => r.CreatedAt, descending)
        };

    // Id is always the final tie-breaker, so rows with equal sort values keep a stable order across pages.
    private static IQueryable<Request> OrderBy<TKey>(
        IQueryable<Request> source, Expression<Func<Request, TKey>> keySelector, bool descending) =>
        descending
            ? source.OrderByDescending(keySelector).ThenByDescending(r => r.Id)
            : source.OrderBy(keySelector).ThenBy(r => r.Id);
}
