namespace DBMSLab.Api.Application.Content;

/// <summary>Signals that a requested course or lesson is missing or cannot be exposed under publication rules.</summary>
/// <param name="message">A client-safe explanation to include in the HTTP 404 response.</param>
/// <remarks>Uses the same error for missing and unpublished content to avoid disclosing hidden records.</remarks>
public sealed class ContentNotFoundException(string message) : Exception(message);
