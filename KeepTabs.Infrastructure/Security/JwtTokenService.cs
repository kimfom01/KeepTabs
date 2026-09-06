using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KeepTabs.Application.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KeepTabs.Infrastructure.Security;

/// <summary>
/// Issues signed JWTs for application-level user accounts.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public string GenerateToken(UserAccount user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email)
        };

        if (user.FirstName is not null)
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (user.LastName is not null)
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: _timeProvider.GetUtcNow().AddMinutes(_options.ExpiryMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
