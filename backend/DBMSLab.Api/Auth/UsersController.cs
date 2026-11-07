using DBMSLab.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using UpdateResult = DBMSLab.Api.Auth.UserProfileService.UpdateResult;

namespace DBMSLab.Api.Auth;

/// <summary>Updates the profile of the authenticated user.</summary>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(UserProfileService profiles) : ControllerBase
{
    /// <summary>Updates the first name, last name, or both on the caller's profile.</summary>
    /// <param name="request">The supplied names. Omitted fields retain their stored values.</param>
    /// <response code="200">The updated profile, in the same format as GET /api/auth/me.</response>
    /// <response code="400">No name was supplied, or the request contains an invalid field or value.</response>
    /// <response code="401">The bearer token is invalid or missing, or the account no longer exists.</response>
    /// <response code="409">Another request updated the profile before this update could be saved.</response>
    /// <response code="500">Identity or the database could not complete the update.</response>
    [HttpPatch("me")]
    [EndpointDescription("Requires a bearer JWT. Provide firstName, lastName, or both; null values are invalid.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> UpdateMe([FromBody] UpdateProfileRequest request) =>
        (await profiles.UpdateAsync(User, request)) switch
        {
            UpdateResult.Updated updated => Ok(updated.Profile),
            UpdateResult.Invalid invalid => InvalidRequest(invalid.Errors),
            UpdateResult.AccountMissing => Unauthorized(),
            UpdateResult.Conflict => Problem(statusCode: StatusCodes.Status409Conflict,
                detail: "The profile changed while this request was in progress. Reload it and try again."),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                detail: "The profile could not be updated.")
        };

    private ActionResult InvalidRequest(Dictionary<string, string[]> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var (field, messages) in errors)
            foreach (var message in messages)
                modelState.AddModelError(field, message);

        return ValidationProblem(modelState);
    }
}
