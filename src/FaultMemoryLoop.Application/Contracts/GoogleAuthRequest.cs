namespace FaultMemoryLoop.Application.Contracts;

/// <summary>
/// What the client sends after completing Google's own sign-in flow: the
/// resulting Google ID token. This system never sees or handles the
/// adviser's Google password — Google's servers already did that part.
/// </summary>
public record GoogleAuthRequest(string IdToken);

public record AuthResponse(string Token, DateTimeOffset ExpiresAt, string Email);
