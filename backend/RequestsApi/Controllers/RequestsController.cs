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
}
