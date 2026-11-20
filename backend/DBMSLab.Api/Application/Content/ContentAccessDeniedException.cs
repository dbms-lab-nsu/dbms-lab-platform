namespace DBMSLab.Api.Application.Content;

/// <summary>Signals that an existing course or lesson is inaccessible to the current caller.</summary>
/// <param name="message">A client-safe explanation to include in the HTTP 403 response.</param>
public sealed class ContentAccessDeniedException(string message) : Exception(message);
