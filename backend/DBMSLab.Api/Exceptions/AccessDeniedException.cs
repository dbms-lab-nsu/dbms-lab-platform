namespace DBMSLab.Api.Exceptions;

/// <summary>Exception thrown when the user has no access to the requested resource.</summary>
public sealed class AccessDeniedException : Exception
{
    public AccessDeniedException(string message) : base(message) { }
}
