namespace DBMSLab.Api.Contracts;

/// <summary>Unified error response body format for the API.</summary>
public sealed record ErrorResponse(string Error, string Message, int StatusCode);
