using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DBMSLab.Api.Contracts;
using DBMSLab.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace DBMSLab.Api.Auth;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public LoginResponse Create(AppUser user, IList<string> roles)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "dbms-lab";
        var audience = configuration["Jwt:Audience"] ?? "dbms-lab-client";
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");

        var expires = DateTime.UtcNow.AddHours(1);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(issuer, audience, claims, expires: expires,
            signingCredentials: credentials);
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(jwt), "Bearer", expires);
    }
}
