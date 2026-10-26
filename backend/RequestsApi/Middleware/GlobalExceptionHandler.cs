using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RequestsBL.Exceptions;

namespace RequestsApi.Middleware;

/// <summary>
/// Turns exceptions into RFC 7807 ProblemDetails responses.
/// Known BL exceptions map to 4xx with their (client-safe) message; anything else is a generic 500.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    // Non-standard but widely used status for "client closed the connection" (nginx convention).
    private const int ClientClosedRequest = 499;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client disconnected: nobody is waiting for a response, and it is not a server error.
        // In .NET 8 ExceptionHandlerMiddleware usually handles this case itself (499, Debug log)
        // before calling us; this is a safety net in case it reaches the handler.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client.",
                httpContext.Request.Method, httpContext.Request.Path);
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var problemDetails = CreateProblemDetails(exception);

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            // Method and path only: no query string, headers or body, which may contain sensitive data.
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static ProblemDetails CreateProblemDetails(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors.ToDictionary(e => e.Key, e => e.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = validation.Message
        },
        NotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not found",
            Detail = notFound.Message
        },
        ConflictException conflict => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = conflict.Message
        },
        // Never expose the real message or stack trace of unexpected errors.
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server error",
            Detail = "An unexpected error occurred. Please try again later."
        }
    };
}
