using System.Text.Json.Serialization;

namespace DBMSLab.Api.Contracts;

/// <summary>Specifies one or both name fields to update on the current profile.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateProfileRequest
{
    private string? firstName;
    private string? lastName;

    internal bool HasFirstName { get; private set; }
    internal bool HasLastName { get; private set; }

    /// <summary>The new first name. Omit this field to keep the current value; null is invalid.</summary>
    public string? FirstName
    {
        get => firstName;
        init
        {
            firstName = value;
            HasFirstName = true;
        }
    }

    /// <summary>The new last name. Omit this field to keep the current value; null is invalid.</summary>
    public string? LastName
    {
        get => lastName;
        init
        {
            lastName = value;
            HasLastName = true;
        }
    }
}
