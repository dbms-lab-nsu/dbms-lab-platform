namespace DBMSLab.Api.Application.Content;

/// <summary>Signals that a requested course or lesson does not exist.</summary>
/// <param name="message">A client-safe explanation to include in the HTTP 404 response.</param>
public sealed class ContentNotFoundException(string message) : Exception(message);
