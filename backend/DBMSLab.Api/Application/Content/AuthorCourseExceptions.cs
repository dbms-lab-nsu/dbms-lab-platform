namespace DBMSLab.Api.Application.Content;

/// <summary>Signals that the caller cannot be resolved to an existing account.</summary>
public sealed class ContentAccountRequiredException : Exception;

/// <summary>Signals a course state change or edit that cannot be applied.</summary>
/// <param name="message">A client-safe explanation of the conflict.</param>
public sealed class CourseStateConflictException(string message) : Exception(message);

/// <summary>Contains field errors for an invalid author course request.</summary>
/// <param name="errors">Client-safe messages keyed by JSON field name.</param>
public sealed class AuthorCourseValidationException(IDictionary<string, string[]> errors) : Exception
{
    /// <summary>Gets the validation errors to return to the caller.</summary>
    public IDictionary<string, string[]> Errors { get; } = errors;
}
