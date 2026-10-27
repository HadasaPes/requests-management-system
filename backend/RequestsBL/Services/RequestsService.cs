using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RequestsBL.Dtos;
using RequestsBL.Exceptions;
using RequestsBL.Mapping;
using RequestsBL.Rules;
using RequestsBL.Validation;
using RequestsDAL.Entities;
using RequestsDAL.Repositories;

namespace RequestsBL.Services;

public class RequestsService : IRequestsService
{
    private const string ConflictMessage =
        "The request was changed by someone else. Reload it and try again.";

    // The stats are global (no filters), so one key is enough.
    private const string StatsCacheKey = "requests:stats";
    private static readonly TimeSpan StatsCacheDuration = TimeSpan.FromSeconds(60);

    // Incremented on every invalidation. Static because the service is scoped while the cache is shared.
    // A stats calculation stores its result only if no invalidation happened while it ran.
    private static long _statsGeneration;
    private static readonly object StatsCacheLock = new();

    private readonly IRequestsRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RequestsService> _logger;

    public RequestsService(IRequestsRepository repository, IMemoryCache cache, ILogger<RequestsService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResponse<RequestDto>> GetRequestsAsync(
        RequestQueryParameters parameters, CancellationToken cancellationToken)
    {
        var query = RequestQueryValidator.ToQuery(parameters);

        var result = await _repository.GetPagedAsync(query, cancellationToken);

        return new PagedResponse<RequestDto>
        {
            Items = result.Items.Select(r => r.ToDto()).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)query.PageSize)
        };
    }

    public async Task<RequestDto> GetRequestByIdAsync(int id, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFound(id);

        return request.ToDto();
    }

    public async Task<RequestStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(StatsCacheKey, out RequestStatsDto? cached))
        {
            _logger.LogDebug("Stats cache hit.");
            return cached!;
        }

        _logger.LogDebug("Stats cache miss.");
        var generation = Interlocked.Read(ref _statsGeneration);
        var stats = (await _repository.GetCountsAsync(cancellationToken)).ToStatsDto();

        lock (StatsCacheLock)
        {
            // If a status changed while the query ran, these numbers may be old: return them, but do not cache them.
            if (_statsGeneration == generation)
            {
                // Absolute expiration: the entry is dropped 60 s after it was stored, even if it is read often.
                _cache.Set(StatsCacheKey, stats, StatsCacheDuration);
            }
        }

        return stats;
    }

    public async Task<RequestDto> UpdateStatusAsync(
        int id, UpdateStatusRequestDto update, CancellationToken cancellationToken)
    {
        var valid = StatusUpdateValidator.Validate(update);

        var request = await _repository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw NotFound(id);

        var previousStatus = request.Status;
        var change = await ApplyStatusChangeAsync(
            request, valid.NewStatus, valid.RowVersion, valid.ChangedBy, cancellationToken);

        switch (change.Outcome)
        {
            case ChangeOutcome.StaleRowVersion:
                _logger.LogWarning("Concurrency conflict on request {RequestId}: the client's rowVersion is stale.", id);
                throw new ConflictException(ConflictMessage);
            case ChangeOutcome.ConflictOnSave:
                _logger.LogWarning("Concurrency conflict on request {RequestId}: the row was changed before saving (detected by the database).", id);
                throw new ConflictException(ConflictMessage);
            case ChangeOutcome.InvalidTransition:
                throw new ValidationException("status", change.Error!);
        }

        // ChangedBy is a person's name, so it is not logged.
        _logger.LogInformation("Request {RequestId} status changed from {PreviousStatus} to {NewStatus}.",
            id, previousStatus, valid.NewStatus);

        return request.ToDto();
    }

    public async Task<BulkStatusUpdateResponseDto> BulkUpdateStatusAsync(
        BulkStatusUpdateRequestDto update, CancellationToken cancellationToken)
    {
        var valid = StatusUpdateValidator.ValidateBulk(update);

        // All rows in one query; ids that do not exist are simply missing from the dictionary.
        var requestsById = (await _repository.GetByIdsForUpdateAsync(
                valid.Items.Select(i => i.Id).ToList(), cancellationToken))
            .ToDictionary(r => r.Id);

        var results = new List<BulkStatusUpdateItemResultDto>(valid.Items.Count);
        var counts = Enum.GetValues<BulkItemOutcome>().ToDictionary(o => o, _ => 0);

        // Items are processed one by one, each in its own transaction, so a failed item never rolls back the others.
        foreach (var item in valid.Items)
        {
            var result = await ProcessBulkItemAsync(
                item, requestsById.GetValueOrDefault(item.Id), valid.NewStatus, valid.ChangedBy, cancellationToken);
            counts[result.Outcome]++;
            results.Add(result.Dto);
        }

        // One summary line per bulk request, not one per item.
        _logger.LogInformation(
            "Bulk status update to {NewStatus}: {Requested} requested, {Updated} updated, {NotFound} not found, {Conflict} conflicts, {InvalidTransition} invalid transitions.",
            valid.NewStatus, valid.Items.Count, counts[BulkItemOutcome.Updated], counts[BulkItemOutcome.NotFound],
            counts[BulkItemOutcome.Conflict], counts[BulkItemOutcome.InvalidTransition]);

        return new BulkStatusUpdateResponseDto
        {
            Requested = valid.Items.Count,
            Succeeded = counts[BulkItemOutcome.Updated],
            Failed = valid.Items.Count - counts[BulkItemOutcome.Updated],
            Results = results
        };
    }

    private async Task<(BulkItemOutcome Outcome, BulkStatusUpdateItemResultDto Dto)> ProcessBulkItemAsync(
        ValidBulkItem item, Request? request, RequestStatus newStatus, string changedBy, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Failed(item.Id, BulkItemOutcome.NotFound, $"Request {item.Id} was not found.");
        }

        var change = await ApplyStatusChangeAsync(request, newStatus, item.RowVersion, changedBy, cancellationToken);

        return change.Outcome switch
        {
            ChangeOutcome.Updated => (BulkItemOutcome.Updated, new BulkStatusUpdateItemResultDto
            {
                Id = item.Id,
                Outcome = nameof(BulkItemOutcome.Updated),
                RowVersion = Convert.ToBase64String(request.RowVersion)
            }),
            ChangeOutcome.InvalidTransition => Failed(item.Id, BulkItemOutcome.InvalidTransition, change.Error!),
            _ => Failed(item.Id, BulkItemOutcome.Conflict, ConflictMessage)
        };

        static (BulkItemOutcome, BulkStatusUpdateItemResultDto) Failed(int id, BulkItemOutcome outcome, string error) =>
            (outcome, new BulkStatusUpdateItemResultDto { Id = id, Outcome = outcome.ToString(), Error = error });
    }

    /// <summary>
    /// The one place where a status change is checked and saved; used by the single and the bulk update.
    /// Does not throw for business outcomes and does not log, so each caller reports in its own way.
    /// </summary>
    private async Task<ChangeResult> ApplyStatusChangeAsync(
        Request request, RequestStatus newStatus, byte[] rowVersion, string changedBy, CancellationToken cancellationToken)
    {
        // The client already has an old version: report a conflict before checking the workflow,
        // so the client is not told "invalid transition" about a status it has not seen yet.
        if (!request.RowVersion.SequenceEqual(rowVersion))
        {
            return new ChangeResult(ChangeOutcome.StaleRowVersion);
        }

        if (StatusTransitions.GetError(request.Status, newStatus) is { } transitionError)
        {
            return new ChangeResult(ChangeOutcome.InvalidTransition, transitionError);
        }

        var result = await _repository.UpdateStatusAsync(
            request, newStatus, rowVersion, changedBy, DateTime.UtcNow, cancellationToken);

        // Conflict here means someone else saved between our read and our UPDATE; the database rejected the UPDATE.
        if (result == StatusUpdateResult.Conflict)
        {
            return new ChangeResult(ChangeOutcome.ConflictOnSave);
        }

        // Every successful change (single or bulk item) goes through here, so the stats are never
        // invalidated by a failed item, and a bulk with zero updates leaves the cache untouched.
        lock (StatsCacheLock)
        {
            _statsGeneration++;
            _cache.Remove(StatsCacheKey);
        }
        return new ChangeResult(ChangeOutcome.Updated);
    }

    public async Task<IReadOnlyList<RequestStatusHistoryDto>> GetHistoryAsync(int id, CancellationToken cancellationToken)
    {
        if (!await _repository.ExistsAsync(id, cancellationToken))
        {
            throw NotFound(id);
        }

        var history = await _repository.GetHistoryAsync(id, cancellationToken);

        return history.Select(h => h.ToDto()).ToList();
    }

    private static NotFoundException NotFound(int id) => new($"Request {id} was not found.");

    private enum ChangeOutcome { Updated, StaleRowVersion, ConflictOnSave, InvalidTransition }

    private readonly record struct ChangeResult(ChangeOutcome Outcome, string? Error = null);

    private enum BulkItemOutcome { Updated, NotFound, Conflict, InvalidTransition }
}
