namespace DBMSLab.Api.Application.Content;

public sealed class ContentNotFoundException(string message) : Exception(message);
