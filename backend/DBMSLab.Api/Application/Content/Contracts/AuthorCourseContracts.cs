using System.Text.Json.Serialization;

namespace DBMSLab.Api.Application.Content.Contracts;

/// <summary>Supplies the required metadata for a new draft course.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateCourseRequest
{
    /// <summary>The course title, up to 200 characters after trimming.</summary>
    public required string Title { get; init; }

    /// <summary>The nonblank course description.</summary>
    public required string Description { get; init; }
}

/// <summary>Supplies one or both course metadata fields. Omitted fields retain their values.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateCourseRequest
{
    private string? title;
    private string? description;

    internal bool HasTitle { get; private set; }
    internal bool HasDescription { get; private set; }

    /// <summary>The new title, up to 200 characters after trimming. Explicit null is invalid.</summary>
    public string? Title
    {
        get => title;
        init
        {
            title = value;
            HasTitle = true;
        }
    }

    /// <summary>The new nonblank description. Explicit null is invalid.</summary>
    public string? Description
    {
        get => description;
        init
        {
            description = value;
            HasDescription = true;
        }
    }
}

/// <summary>Returns a course's owner, editable metadata, and current publication state to its author.</summary>
/// <param name="Id">The course identifier.</param>
/// <param name="AuthorId">The owner assigned from the authenticated account.</param>
/// <param name="Title">The stored course title.</param>
/// <param name="Description">The stored course description.</param>
/// <param name="Status">The state name: Draft, Published, or Archived.</param>
public sealed record AuthorCourseDto(Guid Id, Guid AuthorId, string Title, string Description, string Status);
