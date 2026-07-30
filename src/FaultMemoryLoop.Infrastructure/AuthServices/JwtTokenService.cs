using System.Security.Claims;
using FaultMemoryLoop.Application.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FaultMemoryLoop.Infrastructure.AuthServices;

/// <summary>
/// Issues HMAC-signed JWTs using Microsoft.IdentityModel.JsonWebTokens (the
/// current recommended handler — the older JwtSecurityTokenHandler in
/// System.IdentityModel.Tokens.Jwt is now considered legacy).
///
/// This is the second half of the flow: GoogleTokenVerifier confirms *who*
/// the adviser is; this issues *this system's own* short-lived credential
/// for subsequent API calls, so the API never needs to re-verify a Google
/// token on every request.
/// </summary>
public class JwtTokenService(string signingKey, string issuer, string audience) : ITokenService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public (string Token, DateTimeOffset ExpiresAt) GenerateToken(string subjectId, string email)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

        var securityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, subjectId),
                new Claim(ClaimTypes.Email, email)
            ]),
            Expires = expiresAt.UtcDateTime,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return (token, expiresAt);
    }
}
