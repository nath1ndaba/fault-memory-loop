using FaultMemoryLoop.Application.Interfaces;
using Google.Apis.Auth;

namespace FaultMemoryLoop.Infrastructure.AuthServices;

/// <summary>
/// Verifies a Google-issued ID token using Google's own client library —
/// checks the signature against Google's public keys, confirms the issuer
/// is Google, and confirms the audience matches this application's
/// registered Google OAuth Client ID. This is the real check, not a stand-in:
/// a forged or expired token is rejected by GoogleJsonWebSignature itself.
/// </summary>
public class GoogleTokenVerifier(string googleClientId) : IGoogleTokenVerifier
{
    public async Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [googleClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleIdentity(payload.Subject, payload.Email, payload.Name);
        }
        catch (InvalidJwtException)
        {
            // Expired, malformed, wrong audience, or a signature Google's
            // keys don't back — any of these mean "not a valid Google
            // identity," not an error worth surfacing details about.
            return null;
        }
    }
}
