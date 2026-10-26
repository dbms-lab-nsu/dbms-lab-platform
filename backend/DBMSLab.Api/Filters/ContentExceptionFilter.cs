using DBMSLab.Api.Application.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace DBMSLab.Api.Filters;

/// <summary>Converts failures from content controllers into ProblemDetails without exposing server exception details.</summary>
/// <param name="logger">Records server failures with their request trace identifiers.</param>
public sealed class ContentExceptionFilter(ILogger<ContentExceptionFilter> logger) : IExceptionFilter
{
    /// <summary>Maps access denial to 403, missing content to 404, transient database failures to 503, and other failures to 500.</summary>
    /// <param name="context">The failed MVC action and the HTTP response to populate.</param>
    /// <remarks>
    /// Logs only server failures and adds a trace identifier to handled responses.
    /// Leaves OperationCanceledException unhandled when the request has been aborted.
    /// </remarks>
    public void OnException(ExceptionContext context)
    {
        var httpContext = context.HttpContext;
        var exception = context.Exception;
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return;

        var (status, title, detail) = exception switch
        {
            ContentAccessDeniedException => (
                StatusCodes.Status403Forbidden,
                "Access denied",
                exception.Message),
            ContentNotFoundException => (
                StatusCodes.Status404NotFound,
                "Content not found",
                exception.Message),
            _ when IsDatabaseUnavailable(exception) => (
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable",
                "The database is temporarily unavailable. Please try again later."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Request {TraceId} failed with status {StatusCode}.",
                httpContext.TraceIdentifier, status);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
    }

    /// <summary>Finds transient Npgsql failures or timeouts, including those wrapped by EF Core.</summary>
    /// <param name="exception">The exception at the outermost level of the failure.</param>
    /// <returns>Whether any exception in the inner-exception chain indicates temporary unavailability.</returns>
    private static bool IsDatabaseUnavailable(Exception exception)
    {
        // EF Core can wrap a transient Npgsql error in InvalidOperationException.
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is NpgsqlException { IsTransient: true } or TimeoutException)
                return true;
        }

        return false;
    }
}
