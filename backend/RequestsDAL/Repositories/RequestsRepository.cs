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

    public async Task<RequestCounts> GetCountsAsync(CancellationToken cancellationToken)
    {
        var groups = await _dbContext.Requests
            .GroupBy(r => new { r.Status, r.Priority })
            .Select(g => new { g.Key.Status, g.Key.Priority, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new RequestCounts(
            groups.GroupBy(g => g.Status).ToDictionary(s => s.Key, s => s.Sum(g => g.Count)),
            groups.GroupBy(g => g.Priority).ToDictionary(p => p.Key, p => p.Sum(g => g.Count)));
    }

    public Task<Request?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.Requests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Request>> GetByIdsForUpdateAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await _dbContext.Requests
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

    public async Task<StatusUpdateResult> UpdateStatusAsync(
        Request request,
        RequestStatus newStatus,
        byte[] expectedRowVersion,
        string changedBy,
        DateTime changedAtUtc,
        CancellationToken cancellationToken)
    {
        var history = new RequestStatusHistory
        {
            RequestId = request.Id,
            PreviousStatus = request.Status,
            NewStatus = newStatus,
            ChangedAt = changedAtUtc,
            ChangedBy = changedBy
        };
        _dbContext.RequestStatusHistory.Add(history);

        request.Status = newStatus;
        request.UpdatedAt = changedAtUtc;

        _dbContext.Entry(request).Property(r => r.RowVersion).OriginalValue = expectedRowVersion;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return StatusUpdateResult.Updated;
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(history).State = EntityState.Detached;
            _dbContext.Entry(request).State = EntityState.Detached;
            return StatusUpdateResult.Conflict;
        }
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.Requests.AnyAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RequestStatusHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken) =>
        await _dbContext.RequestStatusHistory
            .AsNoTracking()
            .Where(h => h.RequestId == requestId)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)
            .ToListAsync(cancellationToken);

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

    private static IQueryable<Request> OrderBy<TKey>(
        IQueryable<Request> source, Expression<Func<Request, TKey>> keySelector, bool descending) =>
        descending
            ? source.OrderByDescending(keySelector).ThenByDescending(r => r.Id)
            : source.OrderBy(keySelector).ThenBy(r => r.Id);
}
