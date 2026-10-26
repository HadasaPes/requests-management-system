using Microsoft.Extensions.Logging;
using RequestsBL.Dtos;
using RequestsBL.Exceptions;
using RequestsBL.Mapping;
using RequestsBL.Rules;
using RequestsBL.Validation;
using RequestsDAL.Repositories;

namespace RequestsBL.Services;

public class RequestsService : IRequestsService
{
    private const string ConflictMessage =
        "The request was changed by someone else. Reload it and try again.";

    private readonly IRequestsRepository _repository;
    private readonly ILogger<RequestsService> _logger;

    public RequestsService(IRequestsRepository repository, ILogger<RequestsService> logger)
    {
        _repository = repository;
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
        var counts = await _repository.GetCountsAsync(cancellationToken);

        return counts.ToStatsDto();
    }

    public async Task<RequestDto> UpdateStatusAsync(
        int id, UpdateStatusRequestDto update, CancellationToken cancellationToken)
    {
        var valid = StatusUpdateValidator.Validate(update);

        var request = await _repository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw NotFound(id);

        // The client already has an old version: report a conflict before checking the workflow,
        // so the client is not told "invalid transition" about a status it has not seen yet.
        if (!request.RowVersion.SequenceEqual(valid.RowVersion))
        {
            _logger.LogWarning("Concurrency conflict on request {RequestId}: the client's rowVersion is stale.", id);
            throw new ConflictException(ConflictMessage);
        }

        StatusTransitions.EnsureAllowed(request.Status, valid.NewStatus);

        var previousStatus = request.Status;
        var result = await _repository.UpdateStatusAsync(
            request, valid.NewStatus, valid.RowVersion, valid.ChangedBy, DateTime.UtcNow, cancellationToken);

        // Someone else saved between our read and our UPDATE; the database rejected the UPDATE.
        if (result == StatusUpdateResult.Conflict)
        {
            _logger.LogWarning("Concurrency conflict on request {RequestId}: the row was changed before saving (detected by the database).", id);
            throw new ConflictException(ConflictMessage);
        }

        // ChangedBy is a person's name, so it is not logged.
        _logger.LogInformation("Request {RequestId} status changed from {PreviousStatus} to {NewStatus}.",
            id, previousStatus, valid.NewStatus);

        return request.ToDto();
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
}
