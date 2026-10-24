using DBMSLab.Api.Application.Content;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace DBMSLab.Api.Filters;

public sealed class ContentExceptionFilter(ILogger<ContentExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var httpContext = context.HttpContext;
        var exception = context.Exception;
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return;

        var (status, title, detail) = exception switch
        {
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
