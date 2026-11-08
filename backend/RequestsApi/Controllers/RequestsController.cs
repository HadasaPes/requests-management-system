using Microsoft.AspNetCore.Mvc;
using RequestsBL.Dtos;
using RequestsBL.Services;

namespace RequestsApi.Controllers;

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

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<RequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PagedResponse<RequestDto>>> GetRequests(
        [FromQuery] RequestQueryParameters parameters, CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetRequestsAsync(parameters, cancellationToken));

    [HttpGet("stats")]
    [ProducesResponseType(typeof(RequestStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RequestStatsDto>> GetStats(CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetStatsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<RequestDto>> GetRequestById(int id, CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetRequestByIdAsync(id, cancellationToken));

    [HttpPatch("{id:int}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(RequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<RequestDto>> UpdateStatus(
        int id, [FromBody] UpdateStatusRequestDto update, CancellationToken cancellationToken) =>
        Ok(await _requestsService.UpdateStatusAsync(id, update, cancellationToken));

    [HttpPost("bulk-status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BulkStatusUpdateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<BulkStatusUpdateResponseDto>> BulkUpdateStatus(
        [FromBody] BulkStatusUpdateRequestDto update, CancellationToken cancellationToken) =>
        Ok(await _requestsService.BulkUpdateStatusAsync(update, cancellationToken));

    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<RequestStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<RequestStatusHistoryDto>>> GetHistory(int id, CancellationToken cancellationToken) =>
        Ok(await _requestsService.GetHistoryAsync(id, cancellationToken));
}
