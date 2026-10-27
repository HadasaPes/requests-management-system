using Microsoft.AspNetCore.Mvc;
using RequestsBL.Dtos;
using RequestsBL.Services;

namespace RequestsApi.Controllers;

/// <summary>Service requests.</summary>
[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public class RequestsController : ControllerBase
{
    private readonly IRequestsService _requestsService;

    public RequestsController(IRequestsService requestsService)
    {
        _requestsService = requestsService;
    }

    /// <summary>Lists requests with filters, text search, sorting and paging.</summary>
    /// <response code="200">One page of requests.</response>
    /// <response code="400">One or more query parameters are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PagedResponse<RequestDto>>> GetRequests(
        [FromQuery] RequestQueryParameters parameters, CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetRequestsAsync(parameters, cancellationToken));

    /// <summary>Dashboard summary: number of requests per status and per priority (all requests, no filters).</summary>
    /// <response code="200">Counts, including statuses and priorities with zero requests.</response>
    // "stats" is a literal segment and {id} only matches integers, so the two routes never conflict.
    [HttpGet("stats")]
    [ProducesResponseType(typeof(RequestStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestStatsDto>> GetStats(CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetStatsAsync(cancellationToken));

    /// <summary>Gets one request by id.</summary>
    /// <param name="id">Request id.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The request.</response>
    /// <response code="404">No request with this id exists.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<RequestDto>> GetRequestById(int id, CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetRequestByIdAsync(id, cancellationToken));

    /// <summary>Changes the status of a request (optimistic concurrency with rowVersion).</summary>
    /// <remarks>
    /// Allowed transitions: New → InProgress; InProgress → Waiting or Completed; Waiting → InProgress. Completed is final.
    /// Send the rowVersion from the last GET; the response contains the new rowVersion.
    /// </remarks>
    /// <param name="id">Request id.</param>
    /// <param name="update">New status, rowVersion and who makes the change.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The updated request, with its new rowVersion.</response>
    /// <response code="400">Invalid input, or the transition is not allowed.</response>
    /// <response code="404">No request with this id exists.</response>
    /// <response code="409">The request was changed by someone else since it was read. Reload and retry.</response>
    [HttpPatch("{id:int}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<RequestDto>> UpdateStatus(
        int id, [FromBody] UpdateStatusRequestDto update, CancellationToken cancellationToken) =>
        Ok(await _requestsService.UpdateStatusAsync(id, update, cancellationToken));

    /// <summary>Changes the status of up to 100 requests at once (partial success).</summary>
    /// <remarks>
    /// Each item has its own rowVersion and is saved in its own transaction, so one failed item never
    /// blocks the others. Returns 200 whenever the request itself is valid; check each item's outcome:
    /// Updated (with the new rowVersion), NotFound, Conflict or InvalidTransition (with the reason).
    /// </remarks>
    /// <param name="update">New status, who makes the change, and the items (id + rowVersion).</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">One result per item, in the order they were sent.</response>
    /// <response code="400">The request is invalid (e.g. 0 or more than 100 items, duplicate ids); nothing was changed.</response>
    [HttpPost("bulk-status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BulkStatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<BulkStatusUpdateResponseDto>> BulkUpdateStatus(
        [FromBody] BulkStatusUpdateRequestDto update, CancellationToken cancellationToken) =>
        Ok(await _requestsService.BulkUpdateStatusAsync(update, cancellationToken));

    /// <summary>Status change history of a request, newest first.</summary>
    /// <param name="id">Request id.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The history (empty if the status never changed).</response>
    /// <response code="404">No request with this id exists.</response>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<RequestStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<RequestStatusHistoryDto>>> GetHistory(int id, CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetHistoryAsync(id, cancellationToken));
}
