using System.Security.Claims;
using DBMSLab.Api.Contracts;
using DBMSLab.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace DBMSLab.Api.Auth;

/// <summary>Reads and updates the profile owned by the authenticated caller.</summary>
public sealed class UserProfileService(UserManager<AppUser> users, ILogger<UserProfileService> logger)
{
    /// <summary>Returns the stored profile and roles, or null when the caller has no account.</summary>
    public async Task<UserResponse?> GetAsync(ClaimsPrincipal principal)
    {
        var user = await FindCurrentUserAsync(principal);
        return user is null ? null : CreateResponse(user, await users.GetRolesAsync(user));
    }

    /// <summary>Validates all supplied names before saving them together through Identity.</summary>
    internal async Task<UpdateResult> UpdateAsync(ClaimsPrincipal principal, UpdateProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!request.HasFirstName && !request.HasLastName)
            errors.Add(string.Empty, ["Provide firstName, lastName, or both."]);

        var firstName = request.HasFirstName ? ValidateName(request.FirstName, "firstName", errors) : null;
        var lastName = request.HasLastName ? ValidateName(request.LastName, "lastName", errors) : null;
        if (errors.Count > 0)
            return new UpdateResult.Invalid(errors);

        try
        {
            var user = await FindCurrentUserAsync(principal);
            if (user is null)
                return new UpdateResult.AccountMissing();

            var roles = await users.GetRolesAsync(user);
            if (firstName is not null)
                user.FirstName = firstName;
            if (lastName is not null)
                user.LastName = lastName;

            var result = await users.UpdateAsync(user);
            if (result.Succeeded)
                return new UpdateResult.Updated(CreateResponse(user, roles));

            var codes = result.Errors.Select(error => error.Code).ToArray();
            if (codes.Contains(users.ErrorDescriber.ConcurrencyFailure().Code))
                return new UpdateResult.Conflict();

            logger.LogError("Identity rejected a profile update. Error codes: {ErrorCodes}.",
                string.Join(", ", codes));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "A profile update failed.");
        }

        return new UpdateResult.Failed();
    }

    private Task<AppUser?> FindCurrentUserAsync(ClaimsPrincipal principal) =>
        Guid.TryParse(users.GetUserId(principal), out var id)
            ? users.FindByIdAsync(id.ToString())
            : Task.FromResult<AppUser?>(null);

    private static UserResponse CreateResponse(AppUser user, IList<string> roles) =>
        new(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles);

    private static string? ValidateName(string? value, string field, Dictionary<string, string[]> errors)
    {
        var name = value?.Trim();
        if (string.IsNullOrEmpty(name))
            errors.Add(field, ["Provide a non-empty name."]);
        else if (name.Length > 100)
            errors.Add(field, ["Use at most 100 characters after trimming."]);
        else if (name.Contains('\0'))
            errors.Add(field, ["The name must not contain a null character."]);

        return name;
    }

    internal abstract record UpdateResult
    {
        internal sealed record Updated(UserResponse Profile) : UpdateResult;
        internal sealed record Invalid(Dictionary<string, string[]> Errors) : UpdateResult;
        internal sealed record AccountMissing : UpdateResult;
        internal sealed record Conflict : UpdateResult;
        internal sealed record Failed : UpdateResult;
    }
}
