namespace FaultMemoryLoop.Application.Interfaces;

/// <summary>
/// Verifies a Google-issued ID token — the backend half of "Sign in with
/// Google". The token itself is obtained via Google's OAuth2/OIDC flow on
/// the client side; this interface is what confirms it's genuine (valid
/// signature, correct issuer, correct audience, not expired) before this
/// system trusts the identity inside it.
/// </summary>
public interface IGoogleTokenVerifier
{
    Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken ct = default);
}

/// <summary>The verified identity claims extracted from a valid Google ID token.</summary>
public record GoogleIdentity(string Subject, string Email, string? Name);
